using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using AuthSpike.Certificates;
using AuthSpike.Data;
using AuthSpike.Hosting;
using AuthSpike.Registration;
using AuthSpike.Trust;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers;
using static OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreDefaults;

namespace AuthSpike.AuthServer;

/// <summary>
/// 授權伺服器：OAuth 2.0 Client Credentials + mTLS 用戶端認證（自簽憑證）+ 憑證綁定 Opaque Token + introspection。
/// </summary>
public sealed class AuthServerHost : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly IReadOnlyDictionary<string, AuthServerClient> _clients;
    /// <summary>每個 Client 目前信任的公開憑證（信任名單），與 OpenIddict 登錄的 JWKS 同步更新。</summary>
    private readonly ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> _trustedCertificates;
    private bool _stopped;
    private bool _disposed;

    private AuthServerHost(WebApplication app, int port, IReadOnlyList<AuthServerClient> clients, ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates)
    {
        _app = app;
        Port = port;
        _clients = clients.ToDictionary(client => client.ClientId);
        _trustedCertificates = trustedCertificates;
    }

    public int Port { get; }

    public Uri Issuer => new($"https://localhost:{Port}/");

    /// <summary>目前信任的公開憑證（含已核准加入者）；退役過濾由呼叫端決定。</summary>
    public IReadOnlyList<X509Certificate2> TrustedCertificates(string clientId) => _trustedCertificates[clientId];

    public static async Task<AuthServerHost> StartAsync(int port, SpikeTrust trust, X509Certificate2 serverCertificate, IReadOnlyList<AuthServerClient> clients, TimeSpan accessTokenLifetime, TrustRegistry registry)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenLocalhost(port, listen => listen.UseHttps(https =>
        {
            https.ServerCertificate = serverCertificate;
            // 允許（但不強制）用戶端憑證；是否信任由 OpenIddict 依 tls_client_auth 政策與 Client 登錄判斷。
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.ClientCertificateValidation = static (_, _, _) => true;
        })));

        // 每次啟動使用獨立的 InMemory 資料庫名稱；名稱必須在 lambda 外產生，否則每個 DbContext 會各自一份資料。
        var databaseName = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<SpikeDbContext>(options =>
            options.UseInMemoryDatabase(databaseName).UseOpenIddict());

        // 伺服器登錄的 scope 為所有 Client 核准 scope 的聯集；每個 Client 再依白名單取得自己的權限。
        var registeredScopes = clients.SelectMany(client => client.Scopes).Distinct().ToArray();

        builder.Services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<SpikeDbContext>())
            .AddServer(options =>
            {
                options.SetIssuer(new Uri($"https://localhost:{port}/"));
                options.SetTokenEndpointUris("connect/token")
                       .SetIntrospectionEndpointUris("connect/introspect");

                options.AllowClientCredentialsFlow();
                options.RegisterScopes(registeredScopes);

                // 短效 Token；效期數值為 lab 暫定值，待使用者確認（見 02 單）。
                options.SetAccessTokenLifetime(accessTokenLifetime);

                options.UseReferenceAccessTokens();
                options.UseClientCertificateBoundAccessTokens();

                // Client 以自簽用戶端憑證做 TLS 用戶端認證（RFC 8705 self_signed_tls_client_auth），
                // 憑證公開部分登錄於各 Client 的 JWKS；不另外管理 Client Secret。
                options.EnableSelfSignedTlsClientAuthentication();

                // spike 限定：ephemeral 金鑰。正式環境須改用受管理的簽章／加密憑證。
                options.AddEphemeralEncryptionKey().AddEphemeralSigningKey();

                // token 端點交由應用程式處理 client_credentials，以便簽發 sub = client_id 的 Opaque Token。
                options.UseAspNetCore().EnableTokenEndpointPassthrough();
            });

        var app = builder.Build();

        var audiences = clients.ToDictionary(client => client.ClientId, client => client.Audience);
        var approvedScopes = clients.ToDictionary(client => client.ClientId, client => client.Scopes);
        var trustedCertificates = new ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>>(
            clients.Select(client => new KeyValuePair<string, IReadOnlyList<X509Certificate2>>(client.ClientId, [client.PublicCertificate])));
        var clientsById = clients.ToDictionary(client => client.ClientId);
        var requests = new CertificateRegistrationRequests();
        // 核准與拒絕序列化：避免同一申請被併發核准兩次，或核准與拒絕同時生效。
        var decisionGate = new SemaphoreSlim(1, 1);

        // 管理介面（11 單）：路徑與回應格式為 lab 暫定值，待使用者確認。只接受已登錄為管理員角色的 mTLS 憑證；其他呼叫一律 401。
        app.MapGet("/admin/trust-list", (HttpContext httpContext) =>
        {
            if (!IsAdministrator(httpContext, registry))
            {
                return AdministratorRejected();
            }

            var snapshot = trustedCertificates
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new
                {
                    clientId = pair.Key,
                    enabled = registry.IsClientEnabled(pair.Key),
                    certificateThumbprints = pair.Value.Select(certificate => certificate.Thumbprint).ToArray(),
                });
            return Results.Json(new { clients = snapshot });
        });

        app.MapPost("/admin/clients/{clientId}/disable", (string clientId, HttpContext httpContext) =>
        {
            if (!IsAdministrator(httpContext, registry))
            {
                return AdministratorRejected();
            }

            if (!trustedCertificates.ContainsKey(clientId))
            {
                return Results.Json(new Dictionary<string, string> { ["error"] = "client_not_found" }, statusCode: StatusCodes.Status404NotFound);
            }

            registry.DisableClient(clientId);
            return Results.Json(new { clientId, enabled = false });
        });

        // 憑證登錄申請（12 單）：申請只含 Client 身分與公開憑證；申請本身不改變信任名單，也不能用於取得 Token。
        // 申請管道（工單、人工轉交或自助入口）不在本單範圍；此端點與欄位為 lab 暫定值，待使用者確認。
        app.MapPost("/client-certificate-requests", (CertificateRegistrationSubmission submission) =>
        {
            if (submission.ClientId is null || !trustedCertificates.ContainsKey(submission.ClientId))
            {
                return Results.Json(new Dictionary<string, string> { ["error"] = "client_not_found" }, statusCode: StatusCodes.Status404NotFound);
            }

            if (!CertificateRegistrationRequests.TryParsePublicCertificate(submission.PublicCertificatePem, out var certificate, out var error))
            {
                return Results.Json(new Dictionary<string, string> { ["error"] = error }, statusCode: StatusCodes.Status400BadRequest);
            }

            var request = requests.Submit(submission.ClientId, certificate!);
            return Results.Json(
                new { requestId = request.RequestId, clientId = request.ClientId, status = CertificateRegistrationRequests.ToWire(request.Status) },
                statusCode: StatusCodes.Status202Accepted);
        });

        app.MapGet("/admin/client-certificate-requests/{requestId}", (string requestId, HttpContext httpContext) =>
        {
            if (!IsAdministrator(httpContext, registry))
            {
                return AdministratorRejected();
            }

            var request = FindRequest(requests, requestId);
            if (request is null)
            {
                return Results.Json(new Dictionary<string, string> { ["error"] = "request_not_found" }, statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Json(new
            {
                requestId = request.RequestId,
                clientId = request.ClientId,
                thumbprint = request.PublicCertificate.Thumbprint,
                status = CertificateRegistrationRequests.ToWire(request.Status),
            });
        });

        app.MapPost("/admin/client-certificate-requests/{requestId}/approve", async (string requestId, HttpContext httpContext) =>
        {
            if (!IsAdministrator(httpContext, registry))
            {
                return AdministratorRejected();
            }

            await decisionGate.WaitAsync();
            try
            {
                var request = FindRequest(requests, requestId);
                if (request is null)
                {
                    return Results.Json(new Dictionary<string, string> { ["error"] = "request_not_found" }, statusCode: StatusCodes.Status404NotFound);
                }

                if (request.Status != CertificateRequestStatus.Pending)
                {
                    return RequestNotPending();
                }

                // 先更新信任名單與 OpenIddict 登錄，成功後才標示已核准；失敗時申請維持待核准。
                await ReplaceTrustedCertificatesAsync(
                    app,
                    clientsById,
                    trustedCertificates,
                    request.ClientId,
                    [.. trustedCertificates[request.ClientId], request.PublicCertificate]);
                requests.Decide(request.RequestId, CertificateRequestStatus.Approved);

                return Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "approved" });
            }
            finally
            {
                decisionGate.Release();
            }
        });

        app.MapPost("/admin/client-certificate-requests/{requestId}/reject", async (string requestId, HttpContext httpContext) =>
        {
            if (!IsAdministrator(httpContext, registry))
            {
                return AdministratorRejected();
            }

            await decisionGate.WaitAsync();
            try
            {
                var request = FindRequest(requests, requestId);
                if (request is null)
                {
                    return Results.Json(new Dictionary<string, string> { ["error"] = "request_not_found" }, statusCode: StatusCodes.Status404NotFound);
                }

                if (request.Status != CertificateRequestStatus.Pending)
                {
                    return RequestNotPending();
                }

                // 拒絕只改變申請狀態，不改變信任名單。
                requests.Decide(request.RequestId, CertificateRequestStatus.Rejected);
                return Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "rejected" });
            }
            finally
            {
                decisionGate.Release();
            }
        });

        app.MapPost("/connect/token", async (HttpContext httpContext, IOpenIddictApplicationManager applications) =>
        {
            var request = httpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

            if (!request.IsClientCredentialsGrantType())
            {
                throw new InvalidOperationException("The specified grant type is not supported.");
            }

            var application = await applications.FindByClientIdAsync(request.ClientId!)
                ?? throw new InvalidOperationException("The application cannot be found.");

            var clientId = await applications.GetClientIdAsync(application);
            // 停用的 Client 或已撤銷的 mTLS 憑證不再核發 Token（既有 Token 的接受判斷另由業務 API 檢查）。
            var presented = httpContext.Connection.ClientCertificate;
            // 管理員憑證不是 Client 憑證，不得取得業務 Token（11 單）。
            if (!registry.IsClientEnabled(clientId!) || presented is null || registry.IsCertificateBlocked(presented.Thumbprint) || registry.IsAdministratorCertificate(presented.Thumbprint))
            {
                return Results.Json(new Dictionary<string, string> { ["error"] = Errors.InvalidClient }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var approved = approvedScopes[clientId!];

            // scope 白名單：只能要求 Client 登錄核准的 scope；要求未核准的 scope 直接拒絕，不核發 Token。
            var requested = request.GetScopes();
            if (requested.Any(scope => !approved.Contains(scope)))
            {
                return Results.Json(new Dictionary<string, string> { ["error"] = Errors.InvalidScope }, statusCode: StatusCodes.Status400BadRequest);
            }

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
            identity.SetClaim(Claims.Subject, clientId);
            // 目標 API 由授權伺服器依 Client 登錄決定，呼叫端無法自行指定。
            identity.SetAudiences(audiences[clientId!]);
            // 未指定 scope 時核發該 Client 的全部核准 scope；指定時僅核發所要求的（皆已通過白名單）。
            identity.SetScopes(requested.Length == 0 ? approved : requested);
            identity.SetDestinations(static _ => [Destinations.AccessToken]);

            return Results.SignIn(new ClaimsPrincipal(identity), authenticationScheme: AuthenticationScheme);
        });

        using (var scope = app.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<SpikeDbContext>().Database.EnsureCreatedAsync();

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
            foreach (var client in clients)
            {
                if (await manager.FindByClientIdAsync(client.ClientId) is not null)
                {
                    continue;
                }

                await manager.CreateAsync(BuildDescriptor(client, [client.PublicCertificate]));
            }
        }

        await app.StartAsync();
        return new AuthServerHost(app, port, clients, trustedCertificates);
    }

    /// <summary>管理員核准或拒絕申請時使用的登錄內容；欄位為 lab 暫定值，待使用者確認。</summary>
    private sealed record CertificateRegistrationSubmission(string? ClientId, string? PublicCertificatePem);

    private static CertificateRegistrationRequest? FindRequest(CertificateRegistrationRequests requests, string requestId)
        => Guid.TryParse(requestId, out var id) ? requests.Find(id) : null;

    private static IResult RequestNotPending()
        => Results.Json(new Dictionary<string, string> { ["error"] = "request_not_pending" }, statusCode: StatusCodes.Status409Conflict);

    /// <summary>管理員身分只看出示的 mTLS 憑證是否登錄為管理員角色；與業務 API 的 Token 查證各自獨立。</summary>
    private static bool IsAdministrator(HttpContext httpContext, TrustRegistry registry)
    {
        var certificate = httpContext.Connection.ClientCertificate;
        return certificate is not null && registry.IsAdministratorCertificate(certificate.Thumbprint);
    }

    private static IResult AdministratorRejected()
        => Results.Json(new Dictionary<string, string> { ["error"] = "administrator_authentication_required" }, statusCode: StatusCodes.Status401Unauthorized);

    /// <summary>
    /// 以指定的公開憑證集合取代 Client 登錄的 JWKS（08 單）：登錄新憑證為新增一筆，退役舊憑證則自集合移除。
    /// 集合內每張憑證都能通過該 Client 的 self_signed_tls_client_auth 並取得綁定該憑證的 Token。
    /// </summary>
    public Task SetClientCertificatesAsync(string clientId, IReadOnlyList<X509Certificate2> publicCertificates)
        => ReplaceTrustedCertificatesAsync(_app, _clients, _trustedCertificates, clientId, publicCertificates);

    /// <summary>更新指定 Client 的信任名單與 OpenIddict JWKS；只改動該 Client 的登錄，不影響其他 Client。</summary>
    private static async Task ReplaceTrustedCertificatesAsync(
        WebApplication app,
        IReadOnlyDictionary<string, AuthServerClient> clients,
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        string clientId,
        IReadOnlyList<X509Certificate2> publicCertificates)
    {
        using var scope = app.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await manager.FindByClientIdAsync(clientId)
            ?? throw new InvalidOperationException($"找不到 Client {clientId}。");
        await manager.UpdateAsync(application, BuildDescriptor(clients[clientId], publicCertificates));
        trustedCertificates[clientId] = publicCertificates.ToArray();
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(AuthServerClient client, IEnumerable<X509Certificate2> publicCertificates)
    {
        // 只授予該 Client 核准的 scope 權限；未授予的 scope 由 OpenIddict 拒絕。
        var permissions = new HashSet<string>
        {
            Permissions.Endpoints.Token,
            Permissions.Endpoints.Introspection,
            Permissions.GrantTypes.ClientCredentials,
        };
        permissions.UnionWith(client.Scopes.Select(scope => Permissions.Prefixes.Scope + scope));

        var jwks = new JsonWebKeySet();
        foreach (var certificate in publicCertificates)
        {
            jwks.Keys.Add(JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(SpikeCertificates.PublicPart(certificate))));
        }

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientType = ClientTypes.Confidential,
            DisplayName = client.ClientId,
            JsonWebKeySet = jwks,
        };
        descriptor.Permissions.UnionWith(permissions);
        return descriptor;
    }

    /// <summary>模擬查證服務故障：停止接受連線（之後 introspection 與 Token 端點都無法連線）。</summary>
    public async Task StopAsync()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        await _app.StopAsync();
    }

    /// <summary>管理者撤銷 Token（lab 進程內管理操作，非管理 UI）：撤銷後 introspection 回傳非 active。</summary>
    public async Task RevokeAccessTokenAsync(string accessToken)
    {
        using var scope = _app.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var token = await tokens.FindByReferenceIdAsync(accessToken)
            ?? throw new InvalidOperationException("找不到要撤銷的 Token。");
        await tokens.TryRevokeAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync();
        await _app.DisposeAsync();
    }
}
