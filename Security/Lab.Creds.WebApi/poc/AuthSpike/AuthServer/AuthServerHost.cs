using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using AuthSpike.Certificates;
using AuthSpike.Data;
using AuthSpike.Hosting;
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
    private bool _stopped;
    private bool _disposed;

    private AuthServerHost(WebApplication app, int port)
    {
        _app = app;
        Port = port;
    }

    public int Port { get; }

    public Uri Issuer => new($"https://localhost:{Port}/");

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
            if (!registry.IsClientEnabled(clientId!) || presented is null || registry.IsCertificateRevoked(presented.Thumbprint))
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

                // 只授予該 Client 核准的 scope 權限；未授予的 scope 由 OpenIddict 拒絕。
                var permissions = new HashSet<string>
                {
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.Introspection,
                    Permissions.GrantTypes.ClientCredentials,
                };
                permissions.UnionWith(client.Scopes.Select(scope => Permissions.Prefixes.Scope + scope));

                var descriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = client.ClientId,
                    ClientType = ClientTypes.Confidential,
                    DisplayName = client.ClientId,
                    JsonWebKeySet = new JsonWebKeySet
                    {
                        Keys = { JsonWebKeyConverter.ConvertFromX509SecurityKey(new X509SecurityKey(SpikeCertificates.PublicPart(client.PublicCertificate))) }
                    },
                };
                descriptor.Permissions.UnionWith(permissions);
                await manager.CreateAsync(descriptor);
            }
        }

        await app.StartAsync();
        return new AuthServerHost(app, port);
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
