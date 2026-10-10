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
/// 每個執行環境（EnvironmentName）各自產生獨立的 mTLS 憑證與簽章金鑰（08 單）。
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

    /// <summary>預設執行環境名稱（lab）。</summary>
    public const string DefaultEnvironmentName = "lab";

    private static readonly string[] ClientIdsWithSigningKeys = [OrdersClientId, OrdersPartnerClientId, BillingClientId];

    /// <summary>每個 Client 已登錄過的 mTLS 憑證（含重疊期與已退役者，最後一筆為最新）。</summary>
    private readonly Dictionary<string, List<X509Certificate2>> _clientCertificates;

    /// <summary>每個 Client 已登錄過的請求簽章金鑰（含重疊期與已退役者，最後一筆為最新；持有私鑰，供呼叫端簽署）。</summary>
    private readonly Dictionary<string, List<SignatureKey>> _signatureKeys;

    private readonly X509Certificate2 _serverCertificate;
    private readonly VerificationKeyStore _verificationKeys;
    private readonly TimeSpan _verificationCacheLifetime;

    private SpikeRuntime(
        SpikeTrust trust,
        Dictionary<string, List<X509Certificate2>> clientCertificates,
        Dictionary<string, List<SignatureKey>> signatureKeys,
        X509Certificate2 unregisteredSelfSignedCertificate,
        X509Certificate2 otherCaCertificate,
        AuthServerHost authServer,
        OrdersApiHost ordersApi,
        X509Certificate2 serverCertificate,
        VerificationKeyStore verificationKeys,
        NonceReplayStore replayStore,
        TrustRegistry registry,
        TimeSpan verificationCacheLifetime,
        OrderStore orders,
        SecurityAuditLog auditLog,
        string environmentName)
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
        EnvironmentName = environmentName;
    }

    public SpikeTrust Trust { get; }

    /// <summary>執行環境名稱；mTLS 憑證與簽章金鑰的識別皆含此名稱，不同環境不共用私鑰。</summary>
    public string EnvironmentName { get; }

    /// <summary>自簽但未在授權伺服器登錄的用戶端憑證。</summary>
    public X509Certificate2 UnregisteredSelfSignedCertificate { get; }

    /// <summary>由其他根 CA 簽發的用戶端憑證（非自簽、非本 spike 登錄的方式）。</summary>
    public X509Certificate2 OtherCaCertificate { get; }

    public AuthServerHost AuthServer { get; }

    public OrdersApiHost OrdersApi { get; }

    /// <summary>與主要建立訂單 API 共用的防重放儲存（模擬多執行個體共用同一份狀態）。</summary>
    public NonceReplayStore ReplayStore { get; }

    /// <summary>撤銷與退役狀態的權威來源（授權伺服器與所有業務 API 執行個體共用）。</summary>
    public TrustRegistry Registry { get; }

    /// <summary>訂單資料（主要與第二個執行個體共用，模擬共用的業務資料庫）。</summary>
    public OrderStore Orders { get; }

    /// <summary>安全稽核紀錄（與防重放及訂單儲存分開；主要與第二個執行個體共用）。</summary>
    public SecurityAuditLog AuditLog { get; }

    /// <summary>第二個建立訂單 API 執行個體（僅在需要跨執行個體情境時啟動）。</summary>
    public OrdersApiHost? SecondaryOrdersApi { get; private set; }

    /// <summary>目前最新登錄的 mTLS 用戶端憑證（正常輪替後為新憑證）。</summary>
    public X509Certificate2 ClientCertificate(string clientId) => _clientCertificates[clientId][^1];

    /// <summary>目前最新登錄的請求簽章私鑰（與 mTLS 憑證金鑰分開；正常輪替後為新金鑰）。</summary>
    public SignatureKey SigningKey(string clientId) => _signatureKeys[clientId][^1];

    public static async Task<SpikeRuntime> StartAsync(
        int authServerPort,
        int ordersApiPort,
        TimeSpan? accessTokenLifetime = null,
        TimeSpan? verificationCacheLifetime = null,
        string environmentName = DefaultEnvironmentName)
    {
        var root = SpikeCertificates.CreateRootCertificateAuthority("AuthSpike Root CA");
        var trust = new SpikeTrust(root);
        var serverCertificate = SpikeCertificates.IssueServerCertificate(root, "localhost");

        var clientIds = new[] { OrdersClientId, OrdersPartnerClientId, BillingClientId, ResourceClientId };
        var clientCertificates = clientIds.ToDictionary(
            clientId => clientId,
            clientId => new List<X509Certificate2> { SpikeCertificates.CreateSelfSignedClientCertificate($"{clientId}-{environmentName}") });

        // 請求簽章金鑰：每個呼叫服務、每個環境獨立的 ECDSA P-256 金鑰，與 mTLS 憑證金鑰分開。
        var signatureKeys = ClientIdsWithSigningKeys.ToDictionary(
            clientId => clientId,
            clientId => new List<SignatureKey> { CreateSignatureKey(clientId, environmentName, "1") });

        var unregistered = SpikeCertificates.CreateSelfSignedClientCertificate("unregistered-client");
        var otherRoot = SpikeCertificates.CreateRootCertificateAuthority("Other Root CA");
        var otherCa = SpikeCertificates.IssueCaSignedClientCertificate(otherRoot, OrdersClientId);

        // 授權伺服器只登錄示範 Client 與資源端 orders-api；每個 Client 只核准自己的 scope 白名單。
        // orders-partner-client 與 orders-client 持有相同 scope，用來驗證相同 scope 仍不能越權。
        var registeredClients = new List<AuthServerClient>
        {
            new(OrdersClientId, clientCertificates[OrdersClientId][^1], OrdersAudience, OrdersScopes),
            new(OrdersPartnerClientId, clientCertificates[OrdersPartnerClientId][^1], OrdersAudience, OrdersScopes),
            new(BillingClientId, clientCertificates[BillingClientId][^1], BillingAudience, ["billing.write"]),
            new(ResourceClientId, clientCertificates[ResourceClientId][^1], OrdersAudience, []),
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

        // 業務 API 只持有公開部分，並以 Client 與 KeyId 查找已驗證 Client 自己登錄的金鑰。
        var verificationKeys = new VerificationKeyStore();
        foreach (var (clientId, keys) in signatureKeys)
        {
            foreach (var key in keys)
            {
                verificationKeys.Register(clientId, PublicPart(key));
            }
        }

        var replayStore = new NonceReplayStore();
        var orders = new OrderStore();
        var auditLog = new SecurityAuditLog();
        var ordersApi = await OrdersApiHost.StartAsync(
            ordersApiPort,
            trust,
            serverCertificate,
            authServer.Issuer,
            ResourceClientId,
            clientCertificates[ResourceClientId][^1],
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
            auditLog,
            environmentName);
    }

    /// <summary>啟動第二個建立訂單 API 執行個體，與主要執行個體共用防重放儲存、驗簽金鑰登錄、撤銷狀態與訂單資料。</summary>
    public async Task<OrdersApiHost> StartSecondaryOrdersApiAsync(int port)
    {
        SecondaryOrdersApi = await OrdersApiHost.StartAsync(
            port,
            Trust,
            _serverCertificate,
            AuthServer.Issuer,
            ResourceClientId,
            _clientCertificates[ResourceClientId][^1],
            _verificationKeys,
            ReplayStore,
            Registry,
            _verificationCacheLifetime,
            Orders,
            AuditLog);
        return SecondaryOrdersApi;
    }

    /// <summary>產生新的 mTLS 用戶端憑證（尚未登錄於授權伺服器）。</summary>
    public X509Certificate2 CreateClientCertificate(string clientId)
        => SpikeCertificates.CreateSelfSignedClientCertificate($"{clientId}-{EnvironmentName}-rotated");

    /// <summary>登錄新的 mTLS 用戶端憑證（08 單）：授權伺服器隨即接受該憑證要求 Token；舊憑證在退役前維持有效。</summary>
    public async Task RegisterClientCertificateAsync(string clientId, X509Certificate2 certificate)
    {
        _clientCertificates[clientId].Add(certificate);
        await AuthServer.SetClientCertificatesAsync(clientId, ActiveAuthServerCertificates(clientId));
    }

    /// <summary>退役舊 mTLS 用戶端憑證（08 單）：必須已有其他可用的替代憑證；退役後授權伺服器與業務 API 都不再接受。</summary>
    public async Task RetireClientCertificateAsync(string clientId, X509Certificate2 certificate)
    {
        var hasReplacement = _clientCertificates[clientId].Any(
            candidate => candidate.Thumbprint != certificate.Thumbprint && !Registry.IsCertificateBlocked(candidate.Thumbprint));
        if (!hasReplacement)
        {
            throw new InvalidOperationException("必須先登錄替代的憑證");
        }

        Registry.RetireCertificate(certificate.Thumbprint);
        await AuthServer.SetClientCertificatesAsync(clientId, ActiveAuthServerCertificates(clientId));
    }

    /// <summary>產生新的請求簽章金鑰（尚未登錄於業務 API）。</summary>
    public SignatureKey CreateSigningKey(string clientId)
        => CreateSignatureKey(clientId, EnvironmentName, Guid.NewGuid().ToString("N")[..8]);

    /// <summary>登錄新的請求簽章金鑰（08 單）：業務 API 隨即接受該金鑰；舊金鑰在退役前維持有效。</summary>
    public void RegisterSigningKey(string clientId, SignatureKey key)
    {
        _signatureKeys[clientId].Add(key);
        _verificationKeys.Register(clientId, PublicPart(key));
    }

    /// <summary>退役舊請求簽章金鑰（08 單）：必須已有其他可用的替代金鑰；退役後業務 API 不再接受該金鑰。</summary>
    public void RetireSigningKey(string clientId, SignatureKey key)
    {
        var hasReplacement = _signatureKeys[clientId].Any(
            candidate => candidate.KeyId != key.KeyId && !Registry.IsSigningKeyBlocked(candidate.KeyId));
        if (!hasReplacement)
        {
            throw new InvalidOperationException("必須先登錄替代的金鑰");
        }

        Registry.RetireSigningKey(key.KeyId);
    }

    /// <summary>授權伺服器登錄的 mTLS 公開憑證：所有未退役者（含重疊期的新舊憑證）。</summary>
    private List<X509Certificate2> ActiveAuthServerCertificates(string clientId)
        => _clientCertificates[clientId].Where(certificate => !Registry.IsCertificateRetired(certificate.Thumbprint)).ToList();

    private static SignatureKey CreateSignatureKey(string clientId, string environmentName, string generation)
        => new($"{clientId}-{environmentName}-sig-{generation}", ECDsa.Create(ECCurve.NamedCurves.nistP256));

    /// <summary>僅含公開部分的驗簽金鑰（業務 API 持有）。</summary>
    private static SignatureKey PublicPart(SignatureKey key)
        => new(key.KeyId, ECDsa.Create(key.Key.ExportParameters(includePrivateParameters: false)));

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
