using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AuthSpike.Audit;
using AuthSpike.Certificates;
using AuthSpike.Data;
using AuthSpike.Hosting;
using AuthSpike.Registration;
using AuthSpike.Signing;
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

    public static async Task<AuthServerHost> StartAsync(int port, SpikeTrust trust, X509Certificate2 serverCertificate, IReadOnlyList<AuthServerClient> clients, TimeSpan accessTokenLifetime, TrustRegistry registry, VerificationKeyStore verificationKeys, AdministrativeAuditLog administrativeAudit)
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
        var signingKeyRequests = new SigningKeyRegistrationRequests();
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
                    // 已退役或已撤銷的憑證不列入信任名單（14 單）；JWKS 保留的項目仍由 TrustRegistry 擋下。
                    certificateThumbprints = pair.Value
                        .Where(certificate => !registry.IsCertificateBlocked(certificate.Thumbprint))
                        .Select(certificate => certificate.Thumbprint)
                        .ToArray(),
                });
            return Results.Json(new { clients = snapshot });
        });

        // 管理操作稽核紀錄查詢（15 單）：只有管理員能讀取；讀取不改變信任名單，因此不寫入稽核。路徑與回應格式為 lab 暫定值，待使用者確認。
        app.MapGet("/admin/audit-records", (HttpContext httpContext) =>
        {
            if (!IsAdministrator(httpContext, registry))
            {
                return AdministratorRejected();
            }

            return Results.Json(administrativeAudit.Snapshot().Select(AuditWire).ToArray());
        });

        app.MapPost("/admin/clients/{clientId}/disable", (string clientId, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "client.disable", requireAdministrator: true, () =>
                trustedCertificates.ContainsKey(clientId)
                    ? Accepted(new AdministrativeTarget(clientId), StatusCodes.Status200OK, () =>
                    {
                        registry.DisableClient(clientId);
                        return Task.FromResult(Results.Json(new { clientId, enabled = false }));
                    })
                    : Rejected(new AdministrativeTarget(clientId), StatusCodes.Status404NotFound, "client_not_found")));

        // 退役與撤銷（14 單）：只有已驗證的管理員能執行；路徑與回應格式為 lab 暫定值，待使用者確認。
        // 管理操作與管理員身分檢查先於對象存在性檢查，並與核准、拒絕共用同一決策閘門序列化。
        app.MapPost("/admin/clients/{clientId}/certificates/{thumbprint}/retire", (string clientId, string thumbprint, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "certificate.retire", requireAdministrator: true, () =>
                DecideCertificateChange(trustedCertificates, registry, clientId, thumbprint, retire: true)));

        app.MapPost("/admin/clients/{clientId}/certificates/{thumbprint}/revoke", (string clientId, string thumbprint, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "certificate.revoke", requireAdministrator: true, () =>
                DecideCertificateChange(trustedCertificates, registry, clientId, thumbprint, retire: false)));

        app.MapPost("/admin/clients/{clientId}/signing-keys/{keyId}/retire", (string clientId, string keyId, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "signing_key.retire", requireAdministrator: true, () =>
                DecideSigningKeyChange(trustedCertificates, registry, verificationKeys, clientId, keyId, retire: true)));

        app.MapPost("/admin/clients/{clientId}/signing-keys/{keyId}/revoke", (string clientId, string keyId, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "signing_key.revoke", requireAdministrator: true, () =>
                DecideSigningKeyChange(trustedCertificates, registry, verificationKeys, clientId, keyId, retire: false)));

        // 憑證登錄申請（12 單）：申請只含 Client 身分與公開憑證；申請本身不改變信任名單，也不能用於取得 Token。
        // 申請管道（工單、人工轉交或自助入口）不在本單範圍；此端點與欄位為 lab 暫定值，待使用者確認。
        app.MapPost("/client-certificate-requests", (CertificateRegistrationSubmission submission, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "certificate.submit", requireAdministrator: false, () =>
                DecideCertificateSubmission(trustedCertificates, requests, submission)));

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
                status = request.Status.ToWire(),
            });
        });

        app.MapPost("/admin/client-certificate-requests/{requestId}/approve", (string requestId, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "certificate.approve", requireAdministrator: true, () =>
            {
                var request = FindRequest(requests, requestId);
                if (request is null)
                {
                    return Rejected(new AdministrativeTarget(null), StatusCodes.Status404NotFound, "request_not_found");
                }

                var target = new AdministrativeTarget(request.ClientId, Subject: "certificate", Fingerprint: request.PublicCertificate.Thumbprint);
                if (request.Status != RegistrationRequestStatus.Pending)
                {
                    return Rejected(target, StatusCodes.Status409Conflict, "request_not_pending");
                }

                // 同一指紋若已在任一 Client 的信任名單（含已退役或已撤銷者），核准不會生效也無法區分，因此拒絕（AC-23）。
                if (trustedCertificates.Values.Any(certificates => certificates.Any(candidate => string.Equals(candidate.Thumbprint, request.PublicCertificate.Thumbprint, StringComparison.OrdinalIgnoreCase))))
                {
                    return Rejected(target, StatusCodes.Status409Conflict, "certificate_already_registered");
                }

                return Accepted(target, StatusCodes.Status200OK, async () =>
                {
                    // 先更新信任名單與 OpenIddict 登錄，成功後才標示已核准；失敗時申請維持待核准。
                    await ReplaceTrustedCertificatesAsync(
                        app,
                        clientsById,
                        trustedCertificates,
                        request.ClientId,
                        [.. trustedCertificates[request.ClientId], request.PublicCertificate]);
                    requests.Decide(request.RequestId, RegistrationRequestStatus.Approved);

                    return Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "approved" });
                });
            }));

        app.MapPost("/admin/client-certificate-requests/{requestId}/reject", (string requestId, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "certificate.reject", requireAdministrator: true, () =>
            {
                var request = FindRequest(requests, requestId);
                if (request is null)
                {
                    return Rejected(new AdministrativeTarget(null), StatusCodes.Status404NotFound, "request_not_found");
                }

                var target = new AdministrativeTarget(request.ClientId, Subject: "certificate", Fingerprint: request.PublicCertificate.Thumbprint);
                if (request.Status != RegistrationRequestStatus.Pending)
                {
                    return Rejected(target, StatusCodes.Status409Conflict, "request_not_pending");
                }

                // 拒絕只改變申請狀態，不改變信任名單。
                return Accepted(target, StatusCodes.Status200OK, () =>
                {
                    requests.Decide(request.RequestId, RegistrationRequestStatus.Rejected);
                    return Task.FromResult(Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "rejected" }));
                });
            }));

        // 簽章金鑰登錄申請（13 單）：與憑證申請分開管理；申請只含 Client 身分、keyid 與公開金鑰。待核准前不登錄驗簽金鑰，因此無法通過簽章驗證。
        // 此端點與欄位為 lab 暫定值，待使用者確認。
        app.MapPost("/signing-key-requests", (SigningKeySubmission submission, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "signing_key.submit", requireAdministrator: false, () =>
                DecideSigningKeySubmission(trustedCertificates, signingKeyRequests, submission)));

        app.MapGet("/admin/signing-key-requests/{requestId}", (string requestId, HttpContext httpContext) =>
        {
            if (!IsAdministrator(httpContext, registry))
            {
                return AdministratorRejected();
            }

            var request = Guid.TryParse(requestId, out var id) ? signingKeyRequests.Find(id) : null;
            if (request is null)
            {
                return Results.Json(new Dictionary<string, string> { ["error"] = "request_not_found" }, statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Json(new
            {
                requestId = request.RequestId,
                clientId = request.ClientId,
                keyId = request.PublicKey.KeyId,
                status = request.Status.ToWire(),
            });
        });

        app.MapPost("/admin/signing-key-requests/{requestId}/approve", (string requestId, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "signing_key.approve", requireAdministrator: true, () =>
            {
                var request = Guid.TryParse(requestId, out var id) ? signingKeyRequests.Find(id) : null;
                if (request is null)
                {
                    return Rejected(new AdministrativeTarget(null, Subject: "signing_key"), StatusCodes.Status404NotFound, "request_not_found");
                }

                var target = new AdministrativeTarget(request.ClientId, Subject: "signing_key", KeyId: request.PublicKey.KeyId, Fingerprint: KeyFingerprint(request.PublicKey.Key));
                if (request.Status != RegistrationRequestStatus.Pending)
                {
                    return Rejected(target, StatusCodes.Status409Conflict, "request_not_pending");
                }

                // keyId 若已登錄於任一 Client（含已退役或已撤銷者），登錄後狀態與撤銷共用同一鍵而無法區分，因此拒絕（AC-23）。
                if (verificationKeys.IsKeyIdRegistered(request.PublicKey.KeyId))
                {
                    return Rejected(target, StatusCodes.Status409Conflict, "signing_key_id_in_use");
                }

                return Accepted(target, StatusCodes.Status200OK, () =>
                {
                    // 只登錄到申請它的 Client 名下，且先登錄驗簽金鑰、成功後才標示已核准。
                    verificationKeys.Register(request.ClientId, request.PublicKey);
                    signingKeyRequests.Decide(request.RequestId, RegistrationRequestStatus.Approved);

                    return Task.FromResult(Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "approved" }));
                });
            }));

        app.MapPost("/admin/signing-key-requests/{requestId}/reject", (string requestId, HttpContext httpContext) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, decisionGate, "signing_key.reject", requireAdministrator: true, () =>
            {
                var request = Guid.TryParse(requestId, out var id) ? signingKeyRequests.Find(id) : null;
                if (request is null)
                {
                    return Rejected(new AdministrativeTarget(null, Subject: "signing_key"), StatusCodes.Status404NotFound, "request_not_found");
                }

                var target = new AdministrativeTarget(request.ClientId, Subject: "signing_key", KeyId: request.PublicKey.KeyId, Fingerprint: KeyFingerprint(request.PublicKey.Key));
                if (request.Status != RegistrationRequestStatus.Pending)
                {
                    return Rejected(target, StatusCodes.Status409Conflict, "request_not_pending");
                }

                // 拒絕只改變申請狀態，不登錄驗簽金鑰。
                return Accepted(target, StatusCodes.Status200OK, () =>
                {
                    signingKeyRequests.Decide(request.RequestId, RegistrationRequestStatus.Rejected);
                    return Task.FromResult(Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "rejected" }));
                });
            }));


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

    /// <summary>簽章金鑰登錄申請的內容；欄位為 lab 暫定值，待使用者確認。</summary>
    private sealed record SigningKeySubmission(string? ClientId, string? KeyId, string? PublicKeyPem);

    /// <summary>管理操作稽核的對象（15 單）；未知的對象欄位為 null。Subject 為 certificate 或 signing_key。</summary>
    private sealed record AdministrativeTarget(string? ClientId, string? Subject = null, string? KeyId = null, string? Fingerprint = null);

    /// <summary>
    /// 管理操作的決策（15 單）：計算結果時不改變任何狀態；管理操作稽核寫入成功後才執行 Apply。
    /// </summary>
    private sealed record AdministrativeDecision(AdministrativeTarget Target, int Status, string Reason, Func<Task<IResult>> Apply);

    private const string AdministratorAuthenticationRequired = "administrator_authentication_required";

    private static AdministrativeDecision Accepted(AdministrativeTarget target, int status, Func<Task<IResult>> apply)
        => new(target, status, "ok", apply);

    private static AdministrativeDecision Rejected(AdministrativeTarget target, int status, string error)
        => new(target, status, error, () => Task.FromResult(ErrorResult(status, error)));

    private static CertificateRegistrationRequest? FindRequest(CertificateRegistrationRequests requests, string requestId)
        => Guid.TryParse(requestId, out var id) ? requests.Find(id) : null;

    private static IResult ErrorResult(int statusCode, string error)
        => Results.Json(new Dictionary<string, string> { ["error"] = error }, statusCode: statusCode);

    /// <summary>
    /// 執行一次管理操作（15 單）。先決定結果，寫入管理操作稽核成功後才套用變更；稽核寫入失敗時不套用，並以 503 audit_unavailable 明確回報。
    /// 需要管理員身分的操作，未驗證的呼叫一律 401；稽核紀錄以「未驗證」標示，不記為已驗證管理員。
    /// 管理操作與核准、拒絕、退役、撤銷共用同一決策閘門序列化。
    /// </summary>
    private static async Task<IResult> RunAdministrativeOperationAsync(
        HttpContext httpContext,
        TrustRegistry registry,
        AdministrativeAuditLog audit,
        SemaphoreSlim gate,
        string operation,
        bool requireAdministrator,
        Func<AdministrativeDecision> decide)
    {
        await gate.WaitAsync();
        try
        {
            var administrator = IsAdministrator(httpContext, registry);
            var decision = decide();
            if (requireAdministrator && !administrator)
            {
                decision = new AdministrativeDecision(
                    decision.Target,
                    StatusCodes.Status401Unauthorized,
                    AdministratorAuthenticationRequired,
                    () => Task.FromResult(AdministratorRejected()));
            }

            var record = new AdministrativeAuditRecord(
                CorrelationId: httpContext.TraceIdentifier,
                OccurredAt: DateTimeOffset.UtcNow,
                Operation: operation,
                ActorStatus: administrator ? "已驗證管理員" : "未驗證",
                ActorThumbprint: httpContext.Connection.ClientCertificate?.Thumbprint,
                ClientId: decision.Target.ClientId,
                Subject: decision.Target.Subject,
                KeyId: decision.Target.KeyId,
                Fingerprint: decision.Target.Fingerprint,
                Outcome: decision.Status < StatusCodes.Status400BadRequest ? "accepted" : "rejected",
                Reason: decision.Reason,
                ResultStatus: decision.Status);

            try
            {
                audit.Append(record);
            }
            catch (AdministrativeAuditWriteFailedException)
            {
                return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
            }

            return await decision.Apply();
        }
        finally
        {
            gate.Release();
        }
    }

    private static object AuditWire(AdministrativeAuditRecord record) => new
    {
        correlationId = record.CorrelationId,
        occurredAt = record.OccurredAt,
        operation = record.Operation,
        actorStatus = record.ActorStatus,
        actorThumbprint = record.ActorThumbprint,
        clientId = record.ClientId,
        subject = record.Subject,
        keyId = record.KeyId,
        fingerprint = record.Fingerprint,
        outcome = record.Outcome,
        reason = record.Reason,
        resultStatus = record.ResultStatus,
    };

    /// <summary>簽章金鑰指紋（15 單）：公開金鑰 SubjectPublicKeyInfo 的 SHA-256 十六進位值，不含私鑰。</summary>
    private static string KeyFingerprint(ECDsa key) => Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo()));

    /// <summary>憑證登錄申請的決策（12 單）：只接受不含私鑰的公開憑證；申請本身不改變信任名單。</summary>
    private static AdministrativeDecision DecideCertificateSubmission(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        CertificateRegistrationRequests requests,
        CertificateRegistrationSubmission submission)
    {
        var clientId = submission.ClientId;
        var target = new AdministrativeTarget(clientId, Subject: "certificate");
        if (clientId is null || !trustedCertificates.ContainsKey(clientId))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "client_not_found");
        }

        if (!CertificateRegistrationRequests.TryParsePublicCertificate(submission.PublicCertificatePem, out var certificate, out var error))
        {
            return Rejected(target, StatusCodes.Status400BadRequest, error);
        }

        return Accepted(target with { Fingerprint = certificate!.Thumbprint }, StatusCodes.Status202Accepted, () =>
        {
            var request = requests.Submit(clientId, certificate!);
            return Task.FromResult(Results.Json(
                new { requestId = request.RequestId, clientId = request.ClientId, status = request.Status.ToWire() },
                statusCode: StatusCodes.Status202Accepted));
        });
    }

    /// <summary>簽章金鑰登錄申請的決策（13 單）：只含 Client 身分、keyid 與公開金鑰；待核准前不登錄驗簽金鑰。</summary>
    private static AdministrativeDecision DecideSigningKeySubmission(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        SigningKeyRegistrationRequests signingKeyRequests,
        SigningKeySubmission submission)
    {
        var clientId = submission.ClientId;
        var target = new AdministrativeTarget(clientId, Subject: "signing_key", KeyId: submission.KeyId);
        if (clientId is null || !trustedCertificates.ContainsKey(clientId))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "client_not_found");
        }

        if (!SigningKeyRegistrationRequests.TryParsePublicKey(submission.KeyId, submission.PublicKeyPem, out var key, out var error))
        {
            return Rejected(target, StatusCodes.Status400BadRequest, error);
        }

        return Accepted(target with { Fingerprint = KeyFingerprint(key!.Key) }, StatusCodes.Status202Accepted, () =>
        {
            var request = signingKeyRequests.Submit(clientId, key!);
            return Task.FromResult(Results.Json(
                new { requestId = request.RequestId, clientId = request.ClientId, status = request.Status.ToWire() },
                statusCode: StatusCodes.Status202Accepted));
        });
    }

    /// <summary>
    /// 決定管理員退役或撤銷 Client 的 mTLS 憑證（14 單）。退役需已有其他可用的替代憑證（沿用 08 單的輪替重疊規則）；撤銷不需替代憑證。
    /// 狀態只寫入 TrustRegistry：Token 端點與業務 API 每次請求都讀取該狀態，因此既有連線上的後續請求即時被阻擋。
    /// 不從 OpenIddict JWKS 移除已退役或已撤銷的憑證：OpenIddict 不接受空的 JWKS（會使 self_signed_tls_client_auth 的 Client 更新失敗），
    /// 且已退役或已撤銷的憑證無論出現在 JWKS 與否都會被拒絕。信任名單快照只列出未退役、未撤銷的憑證。
    /// </summary>
    private static AdministrativeDecision DecideCertificateChange(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        TrustRegistry registry,
        string clientId,
        string thumbprint,
        bool retire)
    {
        var target = new AdministrativeTarget(clientId, Subject: "certificate", Fingerprint: thumbprint);
        if (!trustedCertificates.TryGetValue(clientId, out var registered))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "client_not_found");
        }

        var certificate = registered.FirstOrDefault(candidate => string.Equals(candidate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase));
        if (certificate is null)
        {
            return Rejected(target, StatusCodes.Status404NotFound, "certificate_not_found");
        }

        target = target with { Fingerprint = certificate.Thumbprint };

        // 已撤銷或已退役的對象不改變任何狀態，回應明確的狀態衝突。
        if (registry.IsCertificateRevoked(certificate.Thumbprint))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "already_revoked");
        }

        if (registry.IsCertificateRetired(certificate.Thumbprint))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "already_retired");
        }

        // 登錄狀態以憑證指紋為鍵；同一指紋若也登錄於其他 Client，退役或撤銷會波及其他 Client，因此拒絕。
        if (trustedCertificates.Any(pair => pair.Key != clientId && pair.Value.Any(candidate => string.Equals(candidate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "certificate_shared");
        }

        if (retire && !registered.Any(candidate => !string.Equals(candidate.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase) && !registry.IsCertificateBlocked(candidate.Thumbprint)))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "replacement_required");
        }

        return Accepted(target, StatusCodes.Status200OK, () =>
        {
            if (retire)
            {
                registry.RetireCertificate(certificate.Thumbprint);
            }
            else
            {
                registry.RevokeCertificate(certificate.Thumbprint);
            }

            return Task.FromResult(Results.Json(new { clientId, thumbprint = certificate.Thumbprint, status = retire ? "retired" : "revoked" }));
        });
    }

    /// <summary>
    /// 決定管理員退役或撤銷 Client 的請求簽章金鑰（14 單）。退役需已有其他可用的替代金鑰（沿用 08 單的輪替重疊規則）；
    /// 撤銷不需替代金鑰。狀態寫入 TrustRegistry，業務 API 每次請求都讀取，因此既有連線上的後續請求即時被阻擋。
    /// </summary>
    private static AdministrativeDecision DecideSigningKeyChange(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        TrustRegistry registry,
        VerificationKeyStore verificationKeys,
        string clientId,
        string keyId,
        bool retire)
    {
        var target = new AdministrativeTarget(clientId, Subject: "signing_key", KeyId: keyId);
        if (!trustedCertificates.ContainsKey(clientId))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "client_not_found");
        }

        // 已撤銷或已退役的金鑰仍保留於驗簽登錄中，因此先確認歸屬，再依狀態回應衝突。
        if (!verificationKeys.TryGet(clientId, keyId, out var key))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "signing_key_not_found");
        }

        target = target with { Fingerprint = KeyFingerprint(key!.Key) };

        if (registry.IsSigningKeyRevoked(keyId))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "already_revoked");
        }

        if (registry.IsSigningKeyRetired(keyId))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "already_retired");
        }

        if (verificationKeys.IsKeyIdSharedWithOtherClient(clientId, keyId))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "signing_key_shared");
        }

        if (retire && !verificationKeys.KeyIdsOf(clientId).Any(candidate => candidate != keyId && !registry.IsSigningKeyBlocked(candidate)))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "replacement_required");
        }

        return Accepted(target, StatusCodes.Status200OK, () =>
        {
            if (retire)
            {
                registry.RetireSigningKey(keyId);
            }
            else
            {
                registry.RevokeSigningKey(keyId);
            }

            return Task.FromResult(Results.Json(new { clientId, keyId, status = retire ? "retired" : "revoked" }));
        });
    }

    /// <summary>管理員身分只看出示的 mTLS 憑證是否登錄為管理員角色；與業務 API 的 Token 查證各自獨立。</summary>
    private static bool IsAdministrator(HttpContext httpContext, TrustRegistry registry)
    {
        var certificate = httpContext.Connection.ClientCertificate;
        return certificate is not null && registry.IsAdministratorCertificate(certificate.Thumbprint);
    }

    private static IResult AdministratorRejected()
        => Results.Json(new Dictionary<string, string> { ["error"] = AdministratorAuthenticationRequired }, statusCode: StatusCodes.Status401Unauthorized);

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
