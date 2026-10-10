using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AuthSpike.AuthServer;
using AuthSpike.Audit;
using AuthSpike.Certificates;
using AuthSpike.OrdersApi;
using AuthSpike.Replay;
using AuthSpike.Signing;
using AuthSpike.Trust;

namespace AuthSpike.Hosting;

/// <summary>
/// spike 的組合根：產生測試憑證，並以真實 TCP／TLS 啟動授權伺服器與建立訂單 API。
/// </summary>
public sealed class SpikeRuntime : IAsyncDisposable
{
    public const string OrdersClientId = "orders-client";
    public const string OrdersPartnerClientId = "orders-partner-client";
    public const string BillingClientId = "billing-client";
    public const string ResourceClientId = "orders-api";
    public const string OrdersAudience = "orders-api";
    public const string BillingAudience = "billing-api";

    /// <summary>訂單 API 的核准 scope：讀取與寫入（取消）訂單。</summary>
    public static readonly string[] OrdersScopes = ["orders.read", "orders.write"];

    /// <summary>Token 效期的 lab 暫定值（秒），待使用者確認；正式值見 02 單。</summary>
    public static readonly TimeSpan DefaultAccessTokenLifetime = TimeSpan.FromSeconds(300);

    /// <summary>業務 API 的 Token 查證快取上限；lab 暫定值，待使用者確認（07 單）。</summary>
    public static readonly TimeSpan DefaultVerificationCacheLifetime = TimeSpan.FromSeconds(10);

    private readonly Dictionary<string, X509Certificate2> _clientCertificates;
    private readonly Dictionary<string, SignatureKey> _signatureKeys;
    private readonly X509Certificate2 _serverCertificate;
    private readonly IReadOnlyDictionary<string, SignatureKey> _verificationKeys;
    private readonly TimeSpan _verificationCacheLifetime;

    private SpikeRuntime(
        SpikeTrust trust,
        Dictionary<string, X509Certificate2> clientCertificates,
        Dictionary<string, SignatureKey> signatureKeys,
        X509Certificate2 unregisteredSelfSignedCertificate,
        X509Certificate2 otherCaCertificate,
        AuthServerHost authServer,
        OrdersApiHost ordersApi,
        X509Certificate2 serverCertificate,
        IReadOnlyDictionary<string, SignatureKey> verificationKeys,
        NonceReplayStore replayStore,
        TrustRegistry registry,
        TimeSpan verificationCacheLifetime,
        OrderStore orders,
        SecurityAuditLog auditLog)
    {
        Trust = trust;
        _clientCertificates = clientCertificates;
        _signatureKeys = signatureKeys;
        _serverCertificate = serverCertificate;
        _verificationKeys = verificationKeys;
        _verificationCacheLifetime = verificationCacheLifetime;
        ReplayStore = replayStore;
        Registry = registry;
        Orders = orders;
        AuditLog = auditLog;
        UnregisteredSelfSignedCertificate = unregisteredSelfSignedCertificate;
        OtherCaCertificate = otherCaCertificate;
        AuthServer = authServer;
        OrdersApi = ordersApi;
    }

    public SpikeTrust Trust { get; }

    /// <summary>自簽但未在授權伺服器登錄的用戶端憑證。</summary>
    public X509Certificate2 UnregisteredSelfSignedCertificate { get; }

    /// <summary>由其他根 CA 簽發的用戶端憑證（非自簽、非本 spike 登錄的方式）。</summary>
    public X509Certificate2 OtherCaCertificate { get; }

    public AuthServerHost AuthServer { get; }

    public OrdersApiHost OrdersApi { get; }

    /// <summary>與主要建立訂單 API 共用的防重放儲存（模擬多執行個體共用同一份狀態）。</summary>
    public NonceReplayStore ReplayStore { get; }

    /// <summary>撤銷狀態的權威來源（授權伺服器與所有業務 API 執行個體共用）。</summary>
    public TrustRegistry Registry { get; }

    /// <summary>訂單資料（主要與第二個執行個體共用，模擬共用的業務資料庫）。</summary>
    public OrderStore Orders { get; }

    /// <summary>安全稽核紀錄（與防重放及訂單儲存分開；主要與第二個執行個體共用）。</summary>
    public SecurityAuditLog AuditLog { get; }

    /// <summary>第二個建立訂單 API 執行個體（僅在需要跨執行個體情境時啟動）。</summary>
    public OrdersApiHost? SecondaryOrdersApi { get; private set; }

    public X509Certificate2 ClientCertificate(string clientId) => _clientCertificates[clientId];

    /// <summary>呼叫服務持有的請求簽章私鑰（與 mTLS 憑證金鑰分開）。</summary>
    public SignatureKey SigningKey(string clientId) => _signatureKeys[clientId];

    public static async Task<SpikeRuntime> StartAsync(
        int authServerPort,
        int ordersApiPort,
        TimeSpan? accessTokenLifetime = null,
        TimeSpan? verificationCacheLifetime = null)
    {
        var root = SpikeCertificates.CreateRootCertificateAuthority("AuthSpike Root CA");
        var trust = new SpikeTrust(root);
        var serverCertificate = SpikeCertificates.IssueServerCertificate(root, "localhost");

        var clientCertificates = new Dictionary<string, X509Certificate2>
        {
            [OrdersClientId] = SpikeCertificates.CreateSelfSignedClientCertificate(OrdersClientId),
            [OrdersPartnerClientId] = SpikeCertificates.CreateSelfSignedClientCertificate(OrdersPartnerClientId),
            [BillingClientId] = SpikeCertificates.CreateSelfSignedClientCertificate(BillingClientId),
            [ResourceClientId] = SpikeCertificates.CreateSelfSignedClientCertificate(ResourceClientId),
        };

        // 請求簽章金鑰：每個呼叫服務獨立的 ECDSA P-256 金鑰，與 mTLS 憑證金鑰分開。
        var signatureKeys = new Dictionary<string, SignatureKey>
        {
            [OrdersClientId] = CreateSignatureKey(OrdersClientId),
            [OrdersPartnerClientId] = CreateSignatureKey(OrdersPartnerClientId),
            [BillingClientId] = CreateSignatureKey(BillingClientId),
        };

        var unregistered = SpikeCertificates.CreateSelfSignedClientCertificate("unregistered-client");
        var otherRoot = SpikeCertificates.CreateRootCertificateAuthority("Other Root CA");
        var otherCa = SpikeCertificates.IssueCaSignedClientCertificate(otherRoot, OrdersClientId);

        // 授權伺服器只登錄示範 Client 與資源端 orders-api；每個 Client 只核准自己的 scope 白名單。
        // orders-partner-client 與 orders-client 持有相同 scope，用來驗證相同 scope 仍不能越權。
        var registeredClients = new List<AuthServerClient>
        {
            new(OrdersClientId, clientCertificates[OrdersClientId], OrdersAudience, OrdersScopes),
            new(OrdersPartnerClientId, clientCertificates[OrdersPartnerClientId], OrdersAudience, OrdersScopes),
            new(BillingClientId, clientCertificates[BillingClientId], BillingAudience, ["billing.write"]),
            new(ResourceClientId, clientCertificates[ResourceClientId], OrdersAudience, []),
        };

        var registry = new TrustRegistry();
        var cacheLifetime = verificationCacheLifetime ?? DefaultVerificationCacheLifetime;
        var authServer = await AuthServerHost.StartAsync(
            authServerPort,
            trust,
            serverCertificate,
            registeredClients,
            accessTokenLifetime ?? DefaultAccessTokenLifetime,
            registry);

        // 業務 API 只持有公開部分，並以 KeyId 對應已驗證 Client。
        var verificationKeys = signatureKeys.ToDictionary(
            pair => pair.Key,
            pair => new SignatureKey(pair.Value.KeyId, ECDsa.Create(pair.Value.Key.ExportParameters(includePrivateParameters: false))));
        var replayStore = new NonceReplayStore();
        var orders = new OrderStore();
        var auditLog = new SecurityAuditLog();
        var ordersApi = await OrdersApiHost.StartAsync(
            ordersApiPort,
            trust,
            serverCertificate,
            authServer.Issuer,
            ResourceClientId,
            clientCertificates[ResourceClientId],
            verificationKeys,
            replayStore,
            registry,
            cacheLifetime,
            orders,
            auditLog);

        return new SpikeRuntime(
            trust,
            clientCertificates,
            signatureKeys,
            unregistered,
            otherCa,
            authServer,
            ordersApi,
            serverCertificate,
            verificationKeys,
            replayStore,
            registry,
            cacheLifetime,
            orders,
            auditLog);
    }

    /// <summary>啟動第二個建立訂單 API 執行個體，與主要執行個體共用防重放儲存、驗簽金鑰、撤銷狀態與訂單資料。</summary>
    public async Task<OrdersApiHost> StartSecondaryOrdersApiAsync(int port)
    {
        SecondaryOrdersApi = await OrdersApiHost.StartAsync(
            port,
            Trust,
            _serverCertificate,
            AuthServer.Issuer,
            ResourceClientId,
            _clientCertificates[ResourceClientId],
            _verificationKeys,
            ReplayStore,
            Registry,
            _verificationCacheLifetime,
            Orders,
            AuditLog);
        return SecondaryOrdersApi;
    }

    private static SignatureKey CreateSignatureKey(string clientId)
        => new($"{clientId}-sig-1", ECDsa.Create(ECCurve.NamedCurves.nistP256));

    public async ValueTask DisposeAsync()
    {
        if (SecondaryOrdersApi is not null)
        {
            await SecondaryOrdersApi.DisposeAsync();
        }

        await OrdersApi.DisposeAsync();
        await AuthServer.DisposeAsync();
    }
}
