using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
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
using Microsoft.AspNetCore.DataProtection;
using Npgsql;

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

    public PostgresAuthStore? AuthStore { get; }

    private AuthServerHost(WebApplication app, int port, IReadOnlyList<AuthServerClient> clients, ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates, PostgresAuthStore? authStore = null, Uri? issuer = null)
    {
        _app = app;
        Port = port;
        Issuer = issuer ?? new($"https://localhost:{port}/");
        _clients = clients.ToDictionary(client => client.ClientId);
        _trustedCertificates = trustedCertificates;
        AuthStore = authStore;
    }

    public int Port { get; }

    public Uri Issuer { get; }

    public Uri BaseUrl => new($"https://localhost:{Port}/");

    /// <summary>目前信任的公開憑證（含已核准加入者）；退役過濾由呼叫端決定。</summary>
    public IReadOnlyList<X509Certificate2> TrustedCertificates(string clientId) => _trustedCertificates[clientId];

    public static async Task<AuthServerHost> StartAsync(
        int port,
        SpikeTrust trust,
        X509Certificate2 serverCertificate,
        IReadOnlyList<AuthServerClient> clients,
        TimeSpan accessTokenLifetime,
        TrustRegistry registry,
        VerificationKeyStore verificationKeys,
        AdministrativeAuditLog administrativeAudit,
        TimeProvider clock,
        string? authDbConnectionString = null,
        bool applyMigrations = false,
        Uri? logicalIssuer = null,
        string? initialAdministratorThumbprint = null,
        CancellationToken cancellationToken = default)
    {
        var issuerUri = logicalIssuer ?? new Uri($"https://localhost:{port}/");
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenLocalhost(port, listen => listen.UseHttps(https =>
        {
            https.ServerCertificate = serverCertificate;
            // 允許（但不強制）用戶端憑證；是否信任由 OpenIddict 依 tls_client_auth 政策與 Client 登錄判斷。
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.ClientCertificateValidation = static (_, _, _) => true;
        })));

        PostgresAuthStore? postgresStore = null;
        if (!string.IsNullOrEmpty(authDbConnectionString))
        {
            if (applyMigrations)
            {
                await AuthDbMigrations.ApplyMigrationsAsync(authDbConnectionString, cancellationToken);
            }
            builder.Services.AddDbContextFactory<SpikeDbContext>(options =>
                options.UseNpgsql(authDbConnectionString).UseOpenIddict());
            postgresStore = new PostgresAuthStore(authDbConnectionString);
        }
        else
        {
            // 每次啟動使用獨立的 InMemory 資料庫名稱；名稱必須在 lambda 外產生，否則每個 DbContext 會各自一份資料。
            var databaseName = Guid.NewGuid().ToString();
            builder.Services.AddDbContextFactory<SpikeDbContext>(options =>
                options.UseInMemoryDatabase(databaseName).UseOpenIddict());
        }

        // 伺服器登錄的 scope 為所有 Client 核准 scope 的聯集；每個 Client 再依白名單取得自己的權限。
        var registeredScopes = clients.SelectMany(client => client.Scopes).Distinct().ToArray();

        builder.Services.AddDataProtection()
            .SetApplicationName("AuthSpike")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(Path.GetTempPath(), "authspike-dataprotection-keys")));

        builder.Services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<SpikeDbContext>())
            .AddServer(options =>
            {
                options.SetIssuer(issuerUri);
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

                // 以伺服器憑證作為 Token 的簽章與加密憑證（使重啟後核發之 Token 依然可被驗證）
                options.AddSigningCertificate(serverCertificate)
                       .AddEncryptionCertificate(serverCertificate);

                // token 端點交由應用程式處理 client_credentials，以便簽發 sub = client_id 的 Opaque Token。
                options.UseAspNetCore().EnableTokenEndpointPassthrough();
            });

        var app = builder.Build();

        var audiences = clients.ToDictionary(client => client.ClientId, client => client.Audience);
        var approvedScopes = clients.ToDictionary(client => client.ClientId, client => client.Scopes);
        var trustedCertificates = new ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>>(
            clients.Select(client => new KeyValuePair<string, IReadOnlyList<X509Certificate2>>(client.ClientId, [client.PublicCertificate])));

        if (postgresStore is not null)
        {
            var dict = trustedCertificates.ToDictionary(k => k.Key, v => v.Value.ToList());
            await postgresStore.InitializeOrLoadClientsAsync(
                clients,
                registry,
                verificationKeys,
                dict,
                async (tx, ct) =>
                {
                    using var scope = app.Services.CreateScope();
                    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SpikeDbContext>>();
                    await using var db = await factory.CreateDbContextAsync(ct);
                    db.Database.SetDbConnection(tx.Connection);
                    await db.Database.UseTransactionAsync(tx, ct);

                    var scopedDb = scope.ServiceProvider.GetService<SpikeDbContext>();
                    if (scopedDb is not null && !ReferenceEquals(scopedDb, db))
                    {
                        scopedDb.Database.SetDbConnection(tx.Connection);
                        await scopedDb.Database.UseTransactionAsync(tx, ct);
                    }

                    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
                    foreach (var client in clients)
                    {
                        if (await manager.FindByClientIdAsync(client.ClientId, ct) is null)
                        {
                            await manager.CreateAsync(BuildDescriptor(client, [client.PublicCertificate]), ct);
                        }
                    }
                },
                initialAdministratorThumbprint: initialAdministratorThumbprint,
                cancellationToken: cancellationToken);
            foreach (var (k, v) in dict)
            {
                trustedCertificates[k] = v;
            }
        }

        var clientsById = clients.ToDictionary(client => client.ClientId);
        var requests = new CertificateRegistrationRequests();
        var signingKeyRequests = new SigningKeyRegistrationRequests();
        // 持有證明 nonce 的一次性登錄（lab 暫定，待使用者確認）；與申請共用決策閘門序列化。
        var proofNonces = new ProofNonceStore();
        // 核准與拒絕序列化：避免同一申請被併發核准兩次，或核准與拒絕同時生效。
        var decisionGate = new SemaphoreSlim(1, 1);

        // 管理介面（11 單）：路徑與回應格式為 lab 暫定值，待使用者確認。只接受已登錄為管理員角色的 mTLS 憑證；其他呼叫一律 401。
        app.MapGet("/admin/trust-list", async (HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken))
            {
                return AdministratorRejected();
            }

            if (postgresStore is not null)
            {
                var dbSnapshot = await postgresStore.GetTrustListAsync(cancellationToken);
                return Results.Json(new { clients = dbSnapshot });
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
        app.MapGet("/admin/audit-records", async (HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken))
            {
                return AdministratorRejected();
            }

            if (postgresStore is not null)
            {
                var dbAudits = await postgresStore.SnapshotAuditsAsync(cancellationToken);
                return Results.Json(dbAudits.Select(AuditWire).ToArray());
            }

            return Results.Json(administrativeAudit.Snapshot().Select(AuditWire).ToArray());
        });

        app.MapPost("/admin/clients/{clientId}/disable", (string clientId, HttpContext httpContext, CancellationToken cancellationToken) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "client.disable", requireAdministrator: true, async () =>
            {
                if (postgresStore is not null)
                {
                    var exists = await postgresStore.ClientExistsAsync(clientId, cancellationToken);
                    if (!exists)
                    {
                        return Rejected(new AdministrativeTarget(clientId), StatusCodes.Status404NotFound, "client_not_found");
                    }
                }
                else if (!trustedCertificates.ContainsKey(clientId))
                {
                    return Rejected(new AdministrativeTarget(clientId), StatusCodes.Status404NotFound, "client_not_found");
                }

                return Accepted(new AdministrativeTarget(clientId), StatusCodes.Status200OK, () =>
                {
                    registry.DisableClient(clientId);
                    return Task.FromResult(Results.Json(new { clientId, enabled = false }));
                }, rollback: () => registry.EnableClient(clientId));
            }, cancellationToken));

        // 退役與撤銷（14 單）：只有已驗證的管理員能執行；路徑與回應格式為 lab 暫定值，待使用者確認。
        // 管理操作與管理員身分檢查先於對象存在性檢查，並與核准、拒絕共用同一決策閘門序列化。
        app.MapPost("/admin/clients/{clientId}/certificates/{thumbprint}/retire", (string clientId, string thumbprint, HttpContext httpContext, CancellationToken cancellationToken) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "certificate.retire", requireAdministrator: true, () =>
                DecideCertificateChangeAsync(trustedCertificates, registry, postgresStore, clientId, thumbprint, retire: true, cancellationToken),
                cancellationToken));

        app.MapPost("/admin/clients/{clientId}/certificates/{thumbprint}/revoke", (string clientId, string thumbprint, HttpContext httpContext, CancellationToken cancellationToken) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "certificate.revoke", requireAdministrator: true, () =>
                DecideCertificateChangeAsync(trustedCertificates, registry, postgresStore, clientId, thumbprint, retire: false, cancellationToken),
                cancellationToken));

        app.MapPost("/admin/clients/{clientId}/signing-keys/{keyId}/retire", (string clientId, string keyId, HttpContext httpContext, CancellationToken cancellationToken) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "signing_key.retire", requireAdministrator: true, () =>
                DecideSigningKeyChangeAsync(trustedCertificates, registry, verificationKeys, postgresStore, clientId, keyId, retire: true, cancellationToken),
                cancellationToken));

        app.MapPost("/admin/clients/{clientId}/signing-keys/{keyId}/revoke", (string clientId, string keyId, HttpContext httpContext, CancellationToken cancellationToken) =>
            RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "signing_key.revoke", requireAdministrator: true, () =>
                DecideSigningKeyChangeAsync(trustedCertificates, registry, verificationKeys, postgresStore, clientId, keyId, retire: false, cancellationToken),
                cancellationToken));

        // 憑證登錄申請（12 單）：申請只含 Client 身分與公開憑證；申請本身不改變信任名單，也不能用於取得 Token。
        // 申請管道（工單、人工轉交或自助入口）不在本單範圍；此端點與欄位為 lab 暫定值，待使用者確認。
        // 申請者驗證（17 單）：只接受已登錄且有效的 Client 以現有已核准憑證經 mTLS 提出，且只能替自己申請；未驗證者一律 401。
        app.MapPost("/client-certificate-requests", async (CertificateRegistrationSubmission submission, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (postgresStore is not null)
            {
                var applicant = await AuthenticateApplicantAsync(trustedCertificates, registry, postgresStore, httpContext.Connection.ClientCertificate, cancellationToken);
                var clientId = submission.ClientId;
                if (applicant is null)
                {
                    var rec = new AdministrativeAuditRecord(httpContext.TraceIdentifier, DateTimeOffset.UtcNow, "certificate.submit", "未驗證", httpContext.Connection.ClientCertificate?.Thumbprint, clientId, "certificate", null, null, "rejected", ApplicantAuthenticationRequired, StatusCodes.Status401Unauthorized, false, null);
                    try
                    {
                        await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                    }
                    catch (AdministrativeAuditWriteFailedException)
                    {
                        return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                    }
                    return Results.Json(new Dictionary<string, string> { ["error"] = ApplicantAuthenticationRequired }, statusCode: StatusCodes.Status401Unauthorized);
                }

                if (!string.Equals(applicant.ClientId, clientId, StringComparison.OrdinalIgnoreCase))
                {
                    var rec = new AdministrativeAuditRecord(httpContext.TraceIdentifier, DateTimeOffset.UtcNow, "certificate.submit", "未驗證", httpContext.Connection.ClientCertificate?.Thumbprint, clientId, "certificate", null, null, "rejected", ApplicantMismatch, StatusCodes.Status403Forbidden, true, applicant.ClientId);
                    try
                    {
                        await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                    }
                    catch (AdministrativeAuditWriteFailedException)
                    {
                        return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                    }
                    return Results.Json(new Dictionary<string, string> { ["error"] = ApplicantMismatch }, statusCode: StatusCodes.Status403Forbidden);
                }

                if (!CertificateRegistrationRequests.TryParsePublicCertificate(submission.PublicCertificatePem, out var certificate, out var error))
                {
                    return Results.Json(new Dictionary<string, string> { ["error"] = error }, statusCode: StatusCodes.Status400BadRequest);
                }

                var now = DateTimeOffset.UtcNow;
                var timing = submission.ProofTimestamp;
                var nonce = submission.ProofNonce;
                if (!ProofTimingAccepted(proofNonces, timing, nonce, now)
                    || !ApplicantProof.VerifyCertificate(certificate!, ApplicantProof.CertificateContent(clientId!, applicant.CertificateThumbprint, certificate!.Thumbprint, timing!.Value, nonce!), submission.PossessionProof))
                {
                    var rec = new AdministrativeAuditRecord(httpContext.TraceIdentifier, now, "certificate.submit", "未驗證", httpContext.Connection.ClientCertificate?.Thumbprint, clientId, "certificate", null, certificate?.Thumbprint, "rejected", PossessionProofInvalid, StatusCodes.Status400BadRequest, true, applicant.ClientId);
                    try
                    {
                        await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                    }
                    catch (AdministrativeAuditWriteFailedException)
                    {
                        return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                    }
                    return Results.Json(new Dictionary<string, string> { ["error"] = PossessionProofInvalid }, statusCode: StatusCodes.Status400BadRequest);
                }

                var requestId = Guid.NewGuid().ToString();
                var expiresAt = now.Add(PendingRequestExpiry);
                var auditRecord = new AdministrativeAuditRecord(
                    CorrelationId: httpContext.TraceIdentifier,
                    OccurredAt: now,
                    Operation: "certificate.submit",
                    ActorStatus: "未驗證",
                    ActorThumbprint: httpContext.Connection.ClientCertificate?.Thumbprint,
                    ClientId: clientId,
                    Subject: "certificate",
                    KeyId: null,
                    Fingerprint: certificate!.Thumbprint,
                    Outcome: "accepted",
                    Reason: "ok",
                    ResultStatus: StatusCodes.Status202Accepted,
                    ApplicantAuthenticated: true,
                    ApplicantClientId: applicant.ClientId);

                try
                {
                    var outcome = await postgresStore.SubmitCertificateRequestAsync(
                        clientId!,
                        requestId,
                        submission.PublicCertificatePem!,
                        certificate.Thumbprint,
                        applicant.CertificateThumbprint,
                        nonce!,
                        DateTimeOffset.FromUnixTimeSeconds(timing!.Value),
                        expiresAt,
                        auditRecord,
                        cancellationToken);

                    if (outcome == RequestSubmissionOutcome.DuplicateNonce)
                    {
                        var rec = new AdministrativeAuditRecord(httpContext.TraceIdentifier, now, "certificate.submit", "未驗證", httpContext.Connection.ClientCertificate?.Thumbprint, clientId, "certificate", null, certificate.Thumbprint, "rejected", PossessionProofInvalid, StatusCodes.Status400BadRequest, true, applicant.ClientId);
                        try
                        {
                            await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                        }
                        catch (AdministrativeAuditWriteFailedException)
                        {
                            return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                        }
                        return Results.Json(new Dictionary<string, string> { ["error"] = PossessionProofInvalid }, statusCode: StatusCodes.Status400BadRequest);
                    }
                    if (outcome == RequestSubmissionOutcome.LimitReached)
                    {
                        var rec = new AdministrativeAuditRecord(httpContext.TraceIdentifier, now, "certificate.submit", "未驗證", httpContext.Connection.ClientCertificate?.Thumbprint, clientId, "certificate", null, certificate.Thumbprint, "rejected", PendingRequestLimitReached, StatusCodes.Status429TooManyRequests, true, applicant.ClientId);
                        try
                        {
                            await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                        }
                        catch (AdministrativeAuditWriteFailedException)
                        {
                            return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                        }
                        return Results.Json(new Dictionary<string, string> { ["error"] = PendingRequestLimitReached }, statusCode: StatusCodes.Status429TooManyRequests);
                    }

                    return Results.Json(new { requestId, clientId, status = "pending" }, statusCode: StatusCodes.Status202Accepted);
                }
                catch (AdministrativeAuditWriteFailedException)
                {
                    return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                }
            }

            return await RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "certificate.submit", requireAdministrator: false, () =>
                DecideCertificateSubmission(trustedCertificates, registry, requests, signingKeyRequests, proofNonces, clock, httpContext.Connection.ClientCertificate, submission),
                cancellationToken);
        });

        app.MapGet("/admin/client-certificate-requests/{requestId}", async (string requestId, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken))
            {
                return AdministratorRejected();
            }

            if (postgresStore is not null)
            {
                var req = await postgresStore.GetCertificateRequestAsync(requestId, cancellationToken);
                if (req is null)
                {
                    return Results.Json(new Dictionary<string, string> { ["error"] = "request_not_found" }, statusCode: StatusCodes.Status404NotFound);
                }

                var status = req.Status == "pending" && req.ExpiresAt <= DateTimeOffset.UtcNow ? "expired" : req.Status;
                return Results.Json(new
                {
                    requestId = req.RequestId,
                    clientId = req.ClientId,
                    thumbprint = req.Fingerprint,
                    status,
                    applicantClientId = req.ClientId,
                    applicantThumbprint = req.ApplicantThumbprint,
                });
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
                status = request.Status.Effective(request.ExpiresAt, clock.GetUtcNow()).ToWire(),
                applicantClientId = request.Applicant.ClientId,
                applicantThumbprint = request.Applicant.CertificateThumbprint,
            });
        });

        app.MapPost("/admin/client-certificate-requests/{requestId}/approve", async (string requestId, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (postgresStore is not null)
            {
                if (!await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken))
                {
                    return AdministratorRejected();
                }

                var auditRecord = new AdministrativeAuditRecord(
                    CorrelationId: httpContext.TraceIdentifier,
                    OccurredAt: DateTimeOffset.UtcNow,
                    Operation: "certificate.approve",
                    ActorStatus: "已驗證管理員",
                    ActorThumbprint: httpContext.Connection.ClientCertificate?.Thumbprint,
                    ClientId: null,
                    Subject: "certificate",
                    KeyId: null,
                    Fingerprint: null,
                    Outcome: "accepted",
                    Reason: "ok",
                    ResultStatus: StatusCodes.Status200OK,
                    ApplicantAuthenticated: false,
                    ApplicantClientId: null);

                IReadOnlyList<X509Certificate2>? previousList = null;
                string? targetClientId = null;

                try
                {
                    var outcome = await postgresStore.DecideCertificateRequestAsync(
                        requestId,
                        approve: true,
                        auditRecord,
                        async (clientId, newCert, transaction, ct) =>
                        {
                            targetClientId = clientId;
                            var currentList = trustedCertificates.TryGetValue(clientId, out var l) ? l : [];
                            previousList = currentList;
                            var pendingUpdatedList = currentList.Concat([newCert]).DistinctBy(c => c.Thumbprint, StringComparer.OrdinalIgnoreCase).ToArray();
                            // 在交易內先更新記憶體；若後續或交易失敗由 catch 補償回復
                            trustedCertificates[clientId] = pendingUpdatedList;
                            try
                            {
                                await ReplaceTrustedCertificatesWithTransactionAsync(app, clientsById, clientId, pendingUpdatedList, transaction, ct);
                            }
                            catch
                            {
                                trustedCertificates[clientId] = previousList;
                                throw;
                            }
                        },
                        cancellationToken);

                    return outcome switch
                    {
                        DecisionOutcome.Approved => Results.Json(new { requestId, status = "approved" }),
                        DecisionOutcome.NotFound => ErrorResult(StatusCodes.Status404NotFound, "request_not_found"),
                        DecisionOutcome.NotPending => ErrorResult(StatusCodes.Status409Conflict, "request_not_pending"),
                        DecisionOutcome.AlreadyRegistered => ErrorResult(StatusCodes.Status409Conflict, "certificate_already_registered"),
                        _ => ErrorResult(StatusCodes.Status500InternalServerError, "unknown_outcome"),
                    };
                }
                catch (AdministrativeAuditWriteFailedException)
                {
                    if (targetClientId is not null && previousList is not null)
                    {
                        trustedCertificates[targetClientId] = previousList;
                    }
                    return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (targetClientId is not null && previousList is not null)
                    {
                        trustedCertificates[targetClientId] = previousList;
                    }
                    return ErrorResult(StatusCodes.Status500InternalServerError, "apply_failed");
                }
            }

            return await RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "certificate.approve", requireAdministrator: true, () =>
            {
                var request = FindRequest(requests, requestId);
                if (request is null)
                {
                    return Rejected(new AdministrativeTarget(null), StatusCodes.Status404NotFound, "request_not_found");
                }

                var target = new AdministrativeTarget(request.ClientId, Subject: "certificate", Fingerprint: request.PublicCertificate.Thumbprint);
                if (RejectUnlessPending(target, request.Status, request.ExpiresAt, clock.GetUtcNow()) is { } notPending)
                {
                    return notPending;
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
                        [.. trustedCertificates[request.ClientId], request.PublicCertificate],
                        cancellationToken);
                    requests.Decide(request.RequestId, RegistrationRequestStatus.Approved);

                    return Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "approved" });
                });
            },
            cancellationToken);
        });

        app.MapPost("/admin/client-certificate-requests/{requestId}/reject", async (string requestId, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (postgresStore is not null)
            {
                if (!await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken))
                {
                    return AdministratorRejected();
                }

                var auditRecord = new AdministrativeAuditRecord(
                    CorrelationId: httpContext.TraceIdentifier,
                    OccurredAt: DateTimeOffset.UtcNow,
                    Operation: "certificate.reject",
                    ActorStatus: "已驗證管理員",
                    ActorThumbprint: httpContext.Connection.ClientCertificate?.Thumbprint,
                    ClientId: null,
                    Subject: "certificate",
                    KeyId: null,
                    Fingerprint: null,
                    Outcome: "accepted",
                    Reason: "ok",
                    ResultStatus: StatusCodes.Status200OK,
                    ApplicantAuthenticated: false,
                    ApplicantClientId: null);

                try
                {
                    var outcome = await postgresStore.DecideCertificateRequestAsync(
                        requestId,
                        approve: false,
                        auditRecord,
                        null,
                        cancellationToken);

                    return outcome switch
                    {
                        DecisionOutcome.Rejected => Results.Json(new { requestId, status = "rejected" }),
                        DecisionOutcome.NotFound => ErrorResult(StatusCodes.Status404NotFound, "request_not_found"),
                        DecisionOutcome.NotPending => ErrorResult(StatusCodes.Status409Conflict, "request_not_pending"),
                        _ => ErrorResult(StatusCodes.Status500InternalServerError, "unknown_outcome"),
                    };
                }
                catch (AdministrativeAuditWriteFailedException)
                {
                    return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return ErrorResult(StatusCodes.Status500InternalServerError, "apply_failed");
                }
            }

            return await RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "certificate.reject", requireAdministrator: true, () =>
            {
                var request = FindRequest(requests, requestId);
                if (request is null)
                {
                    return Rejected(new AdministrativeTarget(null), StatusCodes.Status404NotFound, "request_not_found");
                }

                var target = new AdministrativeTarget(request.ClientId, Subject: "certificate", Fingerprint: request.PublicCertificate.Thumbprint);
                if (RejectUnlessPending(target, request.Status, request.ExpiresAt, clock.GetUtcNow()) is { } notPending)
                {
                    return notPending;
                }

                // 拒絕只改變申請狀態，不改變信任名單。
                return Accepted(target, StatusCodes.Status200OK, () =>
                {
                    requests.Decide(request.RequestId, RegistrationRequestStatus.Rejected);
                    return Task.FromResult(Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "rejected" }));
                });
            });
        });

        // 簽章金鑰登錄申請（13 單）：與憑證申請分開管理；申請只含 Client 身分、keyid 與公開金鑰。待核准前不登錄驗簽金鑰，因此無法通過簽章驗證。
        // 此端點與欄位為 lab 暫定值，待使用者確認。
        app.MapPost("/signing-key-requests", async (SigningKeySubmission submission, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (postgresStore is not null)
            {
                var presented = httpContext.Connection.ClientCertificate;
                var applicant = await AuthenticateApplicantAsync(trustedCertificates, registry, postgresStore, presented, cancellationToken);
                var clientId = submission.ClientId;
                var target = new AdministrativeTarget(clientId, Subject: "signing_key", KeyId: submission.KeyId);

                if (RejectUnlessApplicantFor(target, applicant, clientId) is { } rejection)
                {
                    var applicantAuthenticated = applicant is not null;
                    var applicantClientId = applicant?.ClientId;
                    var rec = new AdministrativeAuditRecord(
                        CorrelationId: httpContext.TraceIdentifier,
                        OccurredAt: DateTimeOffset.UtcNow,
                        Operation: "signing_key.submit",
                        ActorStatus: applicantAuthenticated ? "已驗證 Client" : "未驗證",
                        ActorThumbprint: presented?.Thumbprint,
                        ClientId: clientId,
                        Subject: "signing_key",
                        KeyId: submission.KeyId,
                        Fingerprint: null,
                        Outcome: "rejected",
                        Reason: rejection.Reason,
                        ResultStatus: rejection.Status,
                        ApplicantAuthenticated: applicantAuthenticated,
                        ApplicantClientId: applicantClientId);

                    try
                    {
                        await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                    }
                    catch (AdministrativeAuditWriteFailedException)
                    {
                        return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                    }

                    return Results.Json(new Dictionary<string, string> { ["error"] = rejection.Reason }, statusCode: rejection.Status);
                }

                if (!SigningKeyRegistrationRequests.TryParsePublicKey(submission.KeyId, submission.PublicKeyPem, out var key, out var error))
                {
                    return Results.Json(new Dictionary<string, string> { ["error"] = error }, statusCode: StatusCodes.Status400BadRequest);
                }

                var now = DateTimeOffset.UtcNow;
                var timing = submission.ProofTimestamp;
                var nonce = submission.ProofNonce;
                if (!ProofTimingAccepted(proofNonces, timing, nonce, now)
                    || !ApplicantProof.VerifySigningKey(key!.Key, ApplicantProof.SigningKeyContent(clientId!, applicant!.CertificateThumbprint, key.KeyId, key.Key, timing!.Value, nonce!), submission.PossessionProof))
                {
                    var rec = new AdministrativeAuditRecord(httpContext.TraceIdentifier, now, "signing_key.submit", "未驗證", presented?.Thumbprint, clientId, "signing_key", key?.KeyId, KeyFingerprint(key!.Key), "rejected", PossessionProofInvalid, StatusCodes.Status400BadRequest, true, applicant?.ClientId);
                    try
                    {
                        await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                    }
                    catch (AdministrativeAuditWriteFailedException)
                    {
                        return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                    }
                    return Results.Json(new Dictionary<string, string> { ["error"] = PossessionProofInvalid }, statusCode: StatusCodes.Status400BadRequest);
                }

                var requestId = Guid.NewGuid().ToString();
                var expiresAt = now.Add(PendingRequestExpiry);
                var jwkJson = JsonSerializer.Serialize(new
                {
                    kty = JsonWebAlgorithmsKeyTypes.EllipticCurve,
                    crv = JsonWebKeyECTypes.P256,
                    kid = key.KeyId,
                    x = Base64UrlEncoder.Encode(key.Key.ExportParameters(false).Q.X),
                    y = Base64UrlEncoder.Encode(key.Key.ExportParameters(false).Q.Y),
                });

                var auditRecord = new AdministrativeAuditRecord(
                    CorrelationId: httpContext.TraceIdentifier,
                    OccurredAt: now,
                    Operation: "signing_key.submit",
                    ActorStatus: "未驗證",
                    ActorThumbprint: presented?.Thumbprint,
                    ClientId: clientId,
                    Subject: "signing_key",
                    KeyId: key.KeyId,
                    Fingerprint: KeyFingerprint(key.Key),
                    Outcome: "accepted",
                    Reason: "ok",
                    ResultStatus: StatusCodes.Status202Accepted,
                    ApplicantAuthenticated: true,
                    ApplicantClientId: applicant.ClientId);

                try
                {
                    var outcome = await postgresStore.SubmitSigningKeyRequestAsync(
                        clientId!,
                        requestId,
                        jwkJson,
                        key.KeyId,
                        KeyFingerprint(key.Key),
                        applicant.CertificateThumbprint,
                        nonce!,
                        DateTimeOffset.FromUnixTimeSeconds(timing!.Value),
                        expiresAt,
                        auditRecord,
                        cancellationToken);

                    if (outcome == RequestSubmissionOutcome.DuplicateNonce)
                    {
                        var rec = new AdministrativeAuditRecord(httpContext.TraceIdentifier, now, "signing_key.submit", "未驗證", presented?.Thumbprint, clientId, "signing_key", key.KeyId, KeyFingerprint(key.Key), "rejected", PossessionProofInvalid, StatusCodes.Status400BadRequest, true, applicant.ClientId);
                        try
                        {
                            await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                        }
                        catch (AdministrativeAuditWriteFailedException)
                        {
                            return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                        }
                        return Results.Json(new Dictionary<string, string> { ["error"] = PossessionProofInvalid }, statusCode: StatusCodes.Status400BadRequest);
                    }
                    if (outcome == RequestSubmissionOutcome.LimitReached)
                    {
                        var rec = new AdministrativeAuditRecord(httpContext.TraceIdentifier, now, "signing_key.submit", "未驗證", presented?.Thumbprint, clientId, "signing_key", key.KeyId, KeyFingerprint(key.Key), "rejected", PendingRequestLimitReached, StatusCodes.Status429TooManyRequests, true, applicant.ClientId);
                        try
                        {
                            await TryWriteAuditStandaloneAsync(postgresStore, rec, cancellationToken);
                        }
                        catch (AdministrativeAuditWriteFailedException)
                        {
                            return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                        }
                        return Results.Json(new Dictionary<string, string> { ["error"] = PendingRequestLimitReached }, statusCode: StatusCodes.Status429TooManyRequests);
                    }

                    return Results.Json(new { requestId, clientId, status = "pending" }, statusCode: StatusCodes.Status202Accepted);
                }
                catch (AdministrativeAuditWriteFailedException)
                {
                    return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                }
            }

            return await RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "signing_key.submit", requireAdministrator: false, () =>
                DecideSigningKeySubmission(trustedCertificates, registry, requests, signingKeyRequests, proofNonces, clock, httpContext.Connection.ClientCertificate, submission),
                cancellationToken);
        });

        app.MapGet("/admin/signing-key-requests/{requestId}", async (string requestId, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (!await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken))
            {
                return AdministratorRejected();
            }

            if (postgresStore is not null)
            {
                var req = await postgresStore.GetSigningKeyRequestAsync(requestId, cancellationToken);
                if (req is null)
                {
                    return Results.Json(new Dictionary<string, string> { ["error"] = "request_not_found" }, statusCode: StatusCodes.Status404NotFound);
                }

                var status = req.Status == "pending" && req.ExpiresAt <= DateTimeOffset.UtcNow ? "expired" : req.Status;
                return Results.Json(new
                {
                    requestId = req.RequestId,
                    clientId = req.ClientId,
                    keyId = req.KeyId,
                    fingerprint = req.Fingerprint,
                    status,
                    applicantClientId = req.ClientId,
                    applicantThumbprint = req.ApplicantThumbprint,
                });
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
                status = request.Status.Effective(request.ExpiresAt, clock.GetUtcNow()).ToWire(),
                applicantClientId = request.Applicant.ClientId,
                applicantThumbprint = request.Applicant.CertificateThumbprint,
            });
        });

        app.MapPost("/admin/signing-key-requests/{requestId}/approve", async (string requestId, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (postgresStore is not null)
            {
                if (!await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken))
                {
                    return AdministratorRejected();
                }

                var auditRecord = new AdministrativeAuditRecord(
                    CorrelationId: httpContext.TraceIdentifier,
                    OccurredAt: DateTimeOffset.UtcNow,
                    Operation: "signing_key.approve",
                    ActorStatus: "已驗證管理員",
                    ActorThumbprint: httpContext.Connection.ClientCertificate?.Thumbprint,
                    ClientId: null,
                    Subject: "signing_key",
                    KeyId: null,
                    Fingerprint: null,
                    Outcome: "accepted",
                    Reason: "ok",
                    ResultStatus: StatusCodes.Status200OK,
                    ApplicantAuthenticated: false,
                    ApplicantClientId: null);

                string? targetClientId = null;
                SignatureKey? registeredKey = null;

                try
                {
                    var outcome = await postgresStore.DecideSigningKeyRequestAsync(
                        requestId,
                        approve: true,
                        auditRecord,
                        (clientId, keyId, jwkJson, ct) =>
                        {
                            targetClientId = clientId;
                            var jwk = JsonWebKey.Create(jwkJson);
                            var ecdsa = ECDsa.Create(new ECParameters
                            {
                                Curve = ECCurve.NamedCurves.nistP256,
                                Q = new ECPoint
                                {
                                    X = Base64UrlEncoder.DecodeBytes(jwk.X),
                                    Y = Base64UrlEncoder.DecodeBytes(jwk.Y),
                                }
                            });
                            registeredKey = new SignatureKey(keyId, ecdsa);
                            // 在交易內先更新記憶體；若後續或交易失敗由 catch 補償回復
                            verificationKeys.Register(targetClientId, registeredKey);
                            return Task.CompletedTask;
                        },
                        cancellationToken);

                    return outcome switch
                    {
                        DecisionOutcome.Approved => Results.Json(new { requestId, status = "approved" }),
                        DecisionOutcome.NotFound => ErrorResult(StatusCodes.Status404NotFound, "request_not_found"),
                        DecisionOutcome.NotPending => ErrorResult(StatusCodes.Status409Conflict, "request_not_pending"),
                        DecisionOutcome.AlreadyRegistered => ErrorResult(StatusCodes.Status409Conflict, "signing_key_id_in_use"),
                        _ => ErrorResult(StatusCodes.Status500InternalServerError, "unknown_outcome"),
                    };
                }
                catch (AdministrativeAuditWriteFailedException)
                {
                    if (targetClientId is not null && registeredKey is not null)
                    {
                        verificationKeys.Unregister(targetClientId, registeredKey.KeyId);
                    }
                    return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (targetClientId is not null && registeredKey is not null)
                    {
                        verificationKeys.Unregister(targetClientId, registeredKey.KeyId);
                    }
                    return ErrorResult(StatusCodes.Status500InternalServerError, "apply_failed");
                }
            }

            return await RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "signing_key.approve", requireAdministrator: true, () =>
            {
                var request = Guid.TryParse(requestId, out var id) ? signingKeyRequests.Find(id) : null;
                if (request is null)
                {
                    return Rejected(new AdministrativeTarget(null, Subject: "signing_key"), StatusCodes.Status404NotFound, "request_not_found");
                }

                var target = new AdministrativeTarget(request.ClientId, Subject: "signing_key", KeyId: request.PublicKey.KeyId, Fingerprint: KeyFingerprint(request.PublicKey.Key));
                if (RejectUnlessPending(target, request.Status, request.ExpiresAt, clock.GetUtcNow()) is { } notPending)
                {
                    return notPending;
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
            },
            cancellationToken);
        });

        app.MapPost("/admin/signing-key-requests/{requestId}/reject", async (string requestId, HttpContext httpContext, CancellationToken cancellationToken) =>
        {
            if (postgresStore is not null)
            {
                if (!await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken))
                {
                    return AdministratorRejected();
                }

                var auditRecord = new AdministrativeAuditRecord(
                    CorrelationId: httpContext.TraceIdentifier,
                    OccurredAt: DateTimeOffset.UtcNow,
                    Operation: "signing_key.reject",
                    ActorStatus: "已驗證管理員",
                    ActorThumbprint: httpContext.Connection.ClientCertificate?.Thumbprint,
                    ClientId: null,
                    Subject: "signing_key",
                    KeyId: null,
                    Fingerprint: null,
                    Outcome: "accepted",
                    Reason: "ok",
                    ResultStatus: StatusCodes.Status200OK,
                    ApplicantAuthenticated: false,
                    ApplicantClientId: null);

                try
                {
                    var outcome = await postgresStore.DecideSigningKeyRequestAsync(
                        requestId,
                        approve: false,
                        auditRecord,
                        null,
                        cancellationToken);

                    return outcome switch
                    {
                        DecisionOutcome.Rejected => Results.Json(new { requestId, status = "rejected" }),
                        DecisionOutcome.NotFound => ErrorResult(StatusCodes.Status404NotFound, "request_not_found"),
                        DecisionOutcome.NotPending => ErrorResult(StatusCodes.Status409Conflict, "request_not_pending"),
                        _ => ErrorResult(StatusCodes.Status500InternalServerError, "unknown_outcome"),
                    };
                }
                catch (AdministrativeAuditWriteFailedException)
                {
                    return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    return ErrorResult(StatusCodes.Status500InternalServerError, "apply_failed");
                }
            }

            return await RunAdministrativeOperationAsync(httpContext, registry, administrativeAudit, postgresStore, decisionGate, "signing_key.reject", requireAdministrator: true, () =>
            {
                var request = Guid.TryParse(requestId, out var id) ? signingKeyRequests.Find(id) : null;
                if (request is null)
                {
                    return Rejected(new AdministrativeTarget(null, Subject: "signing_key"), StatusCodes.Status404NotFound, "request_not_found");
                }

                var target = new AdministrativeTarget(request.ClientId, Subject: "signing_key", KeyId: request.PublicKey.KeyId, Fingerprint: KeyFingerprint(request.PublicKey.Key));
                if (RejectUnlessPending(target, request.Status, request.ExpiresAt, clock.GetUtcNow()) is { } notPending)
                {
                    return notPending;
                }

                // 拒絕只改變申請狀態，不登錄驗簽金鑰。
                return Accepted(target, StatusCodes.Status200OK, () =>
                {
                    signingKeyRequests.Decide(request.RequestId, RegistrationRequestStatus.Rejected);
                    return Task.FromResult(Results.Json(new { requestId = request.RequestId, clientId = request.ClientId, status = "rejected" }));
                });
            },
            cancellationToken);
        });

        app.MapPost("/connect/token", async (HttpContext httpContext, IOpenIddictApplicationManager applications, CancellationToken cancellationToken) =>
        {
            var request = httpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

            if (!request.IsClientCredentialsGrantType())
            {
                throw new InvalidOperationException("The specified grant type is not supported.");
            }

            var application = await applications.FindByClientIdAsync(request.ClientId!, cancellationToken)
                ?? throw new InvalidOperationException("The application cannot be found.");

            var clientId = await applications.GetClientIdAsync(application, cancellationToken);
            // 停用的 Client 或已撤銷的 mTLS 憑證不再核發 Token（既有 Token 的接受判斷另由業務 API 檢查）。
            var presented = httpContext.Connection.ClientCertificate;
            if (presented is null)
            {
                return Results.Json(new Dictionary<string, string> { ["error"] = Errors.InvalidClient }, statusCode: StatusCodes.Status401Unauthorized);
            }

            if (postgresStore is not null)
            {
                var isEnabled = await postgresStore.IsClientEnabledAsync(clientId!, cancellationToken);
                var isBlocked = await postgresStore.IsCertificateBlockedAsync(presented.Thumbprint, cancellationToken);
                var isAdmin = await postgresStore.IsAdministratorCertificateAsync(presented.Thumbprint, cancellationToken);
                if (!isEnabled || isBlocked || isAdmin)
                {
                    return Results.Json(new Dictionary<string, string> { ["error"] = Errors.InvalidClient }, statusCode: StatusCodes.Status401Unauthorized);
                }
            }
            else
            {
                // 管理員憑證不是 Client 憑證，不得取得業務 Token（11 單）。
                if (!registry.IsClientEnabled(clientId!) || registry.IsCertificateBlocked(presented.Thumbprint) || registry.IsAdministratorCertificate(presented.Thumbprint))
                {
                    return Results.Json(new Dictionary<string, string> { ["error"] = Errors.InvalidClient }, statusCode: StatusCodes.Status401Unauthorized);
                }
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
            if (string.IsNullOrEmpty(authDbConnectionString))
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SpikeDbContext>>();
                await using var db = await factory.CreateDbContextAsync(cancellationToken);
                await db.Database.EnsureCreatedAsync(cancellationToken);

                var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
                foreach (var client in clients)
                {
                    if (await manager.FindByClientIdAsync(client.ClientId, cancellationToken) is null)
                    {
                        await manager.CreateAsync(BuildDescriptor(client, [client.PublicCertificate]), cancellationToken);
                    }
                }
            }
        }

        await app.StartAsync(cancellationToken);
        return new AuthServerHost(app, port, clients, trustedCertificates, postgresStore, issuerUri);
    }

    /// <summary>管理員核准或拒絕申請時使用的登錄內容；欄位為 lab 暫定值，待使用者確認。</summary>
    private sealed record CertificateRegistrationSubmission(string? ClientId, string? PublicCertificatePem, string? PossessionProof, long? ProofTimestamp, string? ProofNonce);

    /// <summary>簽章金鑰登錄申請的內容；欄位為 lab 暫定值，待使用者確認。</summary>
    private sealed record SigningKeySubmission(string? ClientId, string? KeyId, string? PublicKeyPem, string? PossessionProof, long? ProofTimestamp, string? ProofNonce);



    /// <summary>
    /// 管理操作的決策（15 單）：計算結果時不改變任何狀態；管理操作稽核寫入成功後才執行 Apply。
    /// </summary>
    /// <remarks>
    /// 申請者驗證結果（20 單後續）只由申請端點設定；其他管理操作保持 null（不適用）。
    /// 與操作者狀態分開：被拒絕的嘗試即使申請者已通過驗證，操作者仍標為未驗證。
    /// </remarks>
    private sealed record AdministrativeDecision(
        AdministrativeTarget Target,
        int Status,
        string Reason,
        Func<Task<IResult>> Apply,
        Action? Rollback = null)
    {
        public bool? ApplicantAuthenticated { get; init; }

        public string? ApplicantClientId { get; init; }
    }

    private const string AdministratorAuthenticationRequired = "administrator_authentication_required";
    private const string ApplicantAuthenticationRequired = "applicant_authentication_required";
    private const string PossessionProofInvalid = "possession_proof_invalid";
    private const string ApplicantMismatch = "applicant_mismatch";

    /// <summary>每個 Client 的待核准申請數上限（19 單，lab 暫定值，待使用者確認）；憑證與簽章金鑰申請合計計算。</summary>
    private const int PendingRequestLimitPerClient = 5;
    private const string PendingRequestLimitReached = "pending_request_limit_reached";

    /// <summary>待核准申請的逾期時間（19 單的延伸，lab 暫定 24 小時，待使用者確認）：自提出起算，到達即視為已過期，不能再被核准或拒絕，也不再佔用上限。</summary>
    private static readonly TimeSpan PendingRequestExpiry = TimeSpan.FromHours(24);
    private const string RequestExpired = "request_expired";

    private static AdministrativeDecision Accepted(AdministrativeTarget target, int status, Func<Task<IResult>> apply, Action? rollback = null)
        => new(target, status, "ok", apply, rollback);

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
    private static Task<IResult> RunAdministrativeOperationAsync(
        HttpContext httpContext,
        TrustRegistry registry,
        AdministrativeAuditLog audit,
        PostgresAuthStore? postgresStore,
        SemaphoreSlim gate,
        string operation,
        bool requireAdministrator,
        Func<AdministrativeDecision> decide,
        CancellationToken cancellationToken = default)
        => RunAdministrativeOperationAsync(httpContext, registry, audit, postgresStore, gate, operation, requireAdministrator, () => Task.FromResult(decide()), cancellationToken);

    private static async Task<IResult> RunAdministrativeOperationAsync(
        HttpContext httpContext,
        TrustRegistry registry,
        AdministrativeAuditLog audit,
        PostgresAuthStore? postgresStore,
        SemaphoreSlim gate,
        string operation,
        bool requireAdministrator,
        Func<Task<AdministrativeDecision>> decide,
        CancellationToken cancellationToken = default)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            var administrator = await IsAdministratorAsync(httpContext, registry, postgresStore, cancellationToken);
            var decision = await decide();
            if (requireAdministrator && !administrator)
            {
                decision = decision with
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Reason = AdministratorAuthenticationRequired,
                    Apply = () => Task.FromResult(AdministratorRejected()),
                    Rollback = null,
                };
            }

            var outcome = decision.Status < StatusCodes.Status400BadRequest ? "accepted" : "rejected";
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
                Outcome: outcome,
                Reason: decision.Reason,
                ResultStatus: decision.Status,
                ApplicantAuthenticated: decision.ApplicantAuthenticated,
                ApplicantClientId: decision.ApplicantClientId);

            if (postgresStore is not null)
            {
                if (outcome == "accepted")
                {
                    NpgsqlConnection? conn = null;
                    NpgsqlTransaction? tx = null;
                    try
                    {
                        conn = await postgresStore.OpenConnectionAsync(cancellationToken);
                        tx = await conn.BeginTransactionAsync(cancellationToken);

                        await PostgresAuthStore.ApplyAdministrativeStateChangeAsync(conn, tx, operation, decision.Target, cancellationToken);

                        // 先在 DB 交易中套用記憶體更新；若拋出例外，跳至 catch 執行 DB 回滾與 TrustRegistry 補償復原
                        var result = await decision.Apply();

                        // 稽核與狀態變更於同一 DB 交易提交
                        await PostgresAuthStore.WriteAuditInternalAsync(conn, tx, record, cancellationToken);
                        await tx.CommitAsync(cancellationToken);

                        audit.Append(record);
                        return result;
                    }
                    catch (AdministrativeAuditWriteFailedException)
                    {
                        await SafeRollbackAdministrativeTxAsync(tx, cancellationToken);
                        decision.Rollback?.Invoke();
                        return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                    }
                    catch (Exception applyEx) when (applyEx is not OperationCanceledException)
                    {
                        await SafeRollbackAdministrativeTxAsync(tx, cancellationToken);
                        decision.Rollback?.Invoke();
                        try
                        {
                            var failedRecord = record with { Outcome = "failed", Reason = applyEx.Message };
                            await postgresStore.WriteStandaloneFailedAuditAsync(failedRecord, cancellationToken);
                            audit.Append(failedRecord);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                        }

                        return ErrorResult(StatusCodes.Status500InternalServerError, "apply_failed");
                    }
                    finally
                    {
                        if (tx is not null)
                        {
                            await tx.DisposeAsync();
                        }
                        if (conn is not null)
                        {
                            await conn.DisposeAsync();
                        }
                    }
                }
                else
                {
                    try
                    {
                        await using var conn = await postgresStore.OpenConnectionAsync(cancellationToken);
                        await PostgresAuthStore.WriteAuditInternalAsync(conn, null, record, cancellationToken);
                        audit.Append(record);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        return ErrorResult(StatusCodes.Status503ServiceUnavailable, "audit_unavailable");
                    }

                    return await decision.Apply();
                }
            }
            else
            {
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
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task SafeRollbackAdministrativeTxAsync(NpgsqlTransaction? tx, CancellationToken callerCt)
    {
        if (tx is null) return;
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(callerCt, timeoutCts.Token);
        try
        {
            await tx.RollbackAsync(linkedCts.Token);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !callerCt.IsCancellationRequested)
        {
            System.Diagnostics.Trace.TraceWarning("[RunAdministrativeOperationAsync] Rollback timed out: {0}", ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            System.Diagnostics.Trace.TraceWarning("[RunAdministrativeOperationAsync] Rollback failed: {0}", ex.Message);
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
        applicantAuthenticated = record.ApplicantAuthenticated,
        applicantClientId = record.ApplicantClientId,
    };

    /// <summary>簽章金鑰指紋（15 單）：公開金鑰 SubjectPublicKeyInfo 的 SHA-256 十六進位值，不含私鑰。</summary>
    private static string KeyFingerprint(ECDsa key) => Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo()));

    /// <summary>
    /// 憑證登錄申請的決策（12、17 單）：申請者必須是已登錄且有效的 Client（以現有已核准憑證經 mTLS 驗證），且只能替自己申請；
    /// 只接受不含私鑰的公開憑證，並需證明持有該新憑證的私鑰。申請本身不改變信任名單。
    /// 未驗證者回應統一為 401，不區分 Client 是否存在；已驗證者替他人申請回 403，不回傳對方的申請內容。
    /// </summary>
    private static AdministrativeDecision DecideCertificateSubmission(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        TrustRegistry registry,
        CertificateRegistrationRequests requests,
        SigningKeyRegistrationRequests signingKeyRequests,
        ProofNonceStore proofNonces,
        TimeProvider clock,
        X509Certificate2? presented,
        CertificateRegistrationSubmission submission)
    {
        var applicant = AuthenticateApplicant(trustedCertificates, registry, presented);
        return DecideCertificateSubmissionFor(applicant, requests, signingKeyRequests, proofNonces, clock, submission)
            with { ApplicantAuthenticated = applicant is not null, ApplicantClientId = applicant?.ClientId };
    }

    /// <summary>憑證登錄申請在申請者身分已確定後的決策；申請者驗證結果由 <see cref="DecideCertificateSubmission"/> 附加到每一種決策。</summary>
    private static AdministrativeDecision DecideCertificateSubmissionFor(
        ApplicantIdentity? applicant,
        CertificateRegistrationRequests requests,
        SigningKeyRegistrationRequests signingKeyRequests,
        ProofNonceStore proofNonces,
        TimeProvider clock,
        CertificateRegistrationSubmission submission)
    {
        var clientId = submission.ClientId;
        var target = new AdministrativeTarget(clientId, Subject: "certificate");
        if (RejectUnlessApplicantFor(target, applicant, clientId) is { } rejection)
        {
            return rejection;
        }

        if (!CertificateRegistrationRequests.TryParsePublicCertificate(submission.PublicCertificatePem, out var certificate, out var error))
        {
            return Rejected(target, StatusCodes.Status400BadRequest, error);
        }

        target = target with { Fingerprint = certificate!.Thumbprint };
        var now = DateTimeOffset.UtcNow;
        var timing = submission.ProofTimestamp;
        var nonce = submission.ProofNonce;
        if (!ProofTimingAccepted(proofNonces, timing, nonce, now)
            || !ApplicantProof.VerifyCertificate(certificate!, ApplicantProof.CertificateContent(clientId!, applicant!.CertificateThumbprint, certificate!.Thumbprint, timing!.Value, nonce!), submission.PossessionProof))
        {
            return Rejected(target, StatusCodes.Status400BadRequest, PossessionProofInvalid);
        }

        var receivedAt = clock.GetUtcNow();
        if (PendingRequestLimitIsReached(requests, signingKeyRequests, clientId!, receivedAt))
        {
            return Rejected(target, StatusCodes.Status429TooManyRequests, PendingRequestLimitReached);
        }

        return Accepted(target, StatusCodes.Status202Accepted, () =>
        {
            proofNonces.Consume(nonce!, timing!.Value);
            var request = requests.Submit(clientId!, certificate!, applicant, receivedAt + PendingRequestExpiry);
            return Task.FromResult(Results.Json(
                new { requestId = request.RequestId, clientId = request.ClientId, status = request.Status.ToWire() },
                statusCode: StatusCodes.Status202Accepted));
        });
    }

    /// <summary>
    /// 簽章金鑰登錄申請的決策（13、17 單）：申請者驗證同憑證申請；需證明持有新金鑰的私鑰（以金鑰對申請內容簽章）。
    /// 只含 Client 身分、keyid 與公開金鑰；待核准前不登錄驗簽金鑰。
    /// </summary>
    private static AdministrativeDecision DecideSigningKeySubmission(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        TrustRegistry registry,
        CertificateRegistrationRequests requests,
        SigningKeyRegistrationRequests signingKeyRequests,
        ProofNonceStore proofNonces,
        TimeProvider clock,
        X509Certificate2? presented,
        SigningKeySubmission submission)
    {
        var applicant = AuthenticateApplicant(trustedCertificates, registry, presented);
        return DecideSigningKeySubmissionFor(applicant, requests, signingKeyRequests, proofNonces, clock, submission)
            with { ApplicantAuthenticated = applicant is not null, ApplicantClientId = applicant?.ClientId };
    }

    /// <summary>簽章金鑰登錄申請在申請者身分已確定後的決策；申請者驗證結果由 <see cref="DecideSigningKeySubmission"/> 附加到每一種決策。</summary>
    private static AdministrativeDecision DecideSigningKeySubmissionFor(
        ApplicantIdentity? applicant,
        CertificateRegistrationRequests requests,
        SigningKeyRegistrationRequests signingKeyRequests,
        ProofNonceStore proofNonces,
        TimeProvider clock,
        SigningKeySubmission submission)
    {
        var clientId = submission.ClientId;
        var target = new AdministrativeTarget(clientId, Subject: "signing_key", KeyId: submission.KeyId);
        if (RejectUnlessApplicantFor(target, applicant, clientId) is { } rejection)
        {
            return rejection;
        }

        if (!SigningKeyRegistrationRequests.TryParsePublicKey(submission.KeyId, submission.PublicKeyPem, out var key, out var error))
        {
            return Rejected(target, StatusCodes.Status400BadRequest, error);
        }

        target = target with { Fingerprint = KeyFingerprint(key!.Key) };
        var now = DateTimeOffset.UtcNow;
        var timing = submission.ProofTimestamp;
        var nonce = submission.ProofNonce;
        if (!ProofTimingAccepted(proofNonces, timing, nonce, now)
            || !ApplicantProof.VerifySigningKey(key!.Key, ApplicantProof.SigningKeyContent(clientId!, applicant!.CertificateThumbprint, key.KeyId, key.Key, timing!.Value, nonce!), submission.PossessionProof))
        {
            return Rejected(target, StatusCodes.Status400BadRequest, PossessionProofInvalid);
        }

        var receivedAt = clock.GetUtcNow();
        if (PendingRequestLimitIsReached(requests, signingKeyRequests, clientId!, receivedAt))
        {
            return Rejected(target, StatusCodes.Status429TooManyRequests, PendingRequestLimitReached);
        }

        return Accepted(target, StatusCodes.Status202Accepted, () =>
        {
            proofNonces.Consume(nonce!, timing!.Value);
            var request = signingKeyRequests.Submit(clientId!, key!, applicant, receivedAt + PendingRequestExpiry);
            return Task.FromResult(Results.Json(
                new { requestId = request.RequestId, clientId = request.ClientId, status = request.Status.ToWire() },
                statusCode: StatusCodes.Status202Accepted));
        });
    }

    /// <summary>
    /// 持有證明的時間與 nonce 檢查（18 單後續，lab 暫定、待使用者確認）：時間戳須在時間窗內、nonce 須格式合法且尚未使用。
    /// 任一不成立都視為持有證明無效（possession_proof_invalid），且不登錄 nonce，因此被拒絕的嘗試不影響之後以新 nonce 重提。
    /// </summary>
    private static bool ProofTimingAccepted(ProofNonceStore proofNonces, long? timestamp, string? nonce, DateTimeOffset now)
        => timestamp is { } value
            && ApplicantProof.IsWithinWindow(value, now)
            && ApplicantProof.IsValidNonce(nonce)
            && !proofNonces.IsConsumed(nonce!, now);

    /// <summary>
    /// 每個 Client 的待核准申請數是否已達上限（19 單）：憑證與簽章金鑰的待核准申請合計計算，不因分開提出而繞過。
    /// 只計算未逾期的待核准申請；核准、拒絕或逾期後額度即釋放。呼叫端須在決策閘門內呼叫，計數與受理序列化。
    /// </summary>
    private static bool PendingRequestLimitIsReached(
        CertificateRegistrationRequests requests,
        SigningKeyRegistrationRequests signingKeyRequests,
        string clientId,
        DateTimeOffset now)
        => requests.CountPending(clientId, now) + signingKeyRequests.CountPending(clientId, now) >= PendingRequestLimitPerClient;

    /// <summary>
    /// 待核准申請的核准或拒絕前檢查：只有未逾期的待核准申請可被處理；已逾期回 409 request_expired，
    /// 其他非待核准回 409 request_not_pending。回傳 null 表示可以處理。
    /// </summary>
    private static AdministrativeDecision? RejectUnlessPending(AdministrativeTarget target, RegistrationRequestStatus stored, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        var effective = stored.Effective(expiresAt, now);
        if (effective == RegistrationRequestStatus.Expired)
        {
            return Rejected(target, StatusCodes.Status409Conflict, RequestExpired);
        }

        return effective == RegistrationRequestStatus.Pending
            ? null
            : Rejected(target, StatusCodes.Status409Conflict, "request_not_pending");
    }

    /// <summary>
    /// 申請端點的申請者閘門（17、18 單）：未驗證者回 401（統一訊息，不區分 Client 是否存在）；已驗證者替他人申請回 403。
    /// 回傳 null 表示通過閘門，呼叫端繼續處理申請內容。
    /// </summary>
    private static AdministrativeDecision? RejectUnlessApplicantFor(AdministrativeTarget target, ApplicantIdentity? applicant, string? clientId)
    {
        if (applicant is null)
        {
            return Rejected(target, StatusCodes.Status401Unauthorized, ApplicantAuthenticationRequired);
        }

        if (!string.Equals(clientId, applicant.ClientId, StringComparison.Ordinal))
        {
            return Rejected(target, StatusCodes.Status403Forbidden, ApplicantMismatch);
        }

        return null;
    }

    /// <summary>
    /// 申請者身分（17 單）：出示的 mTLS 憑證必須是某 Client 目前登錄的憑證，且未撤銷、未退役，該 Client 未停用。
    /// 管理員憑證、未登錄憑證與待核准憑證都不是申請者身分；回傳 null 時呼叫端一律回 401。
    /// </summary>
    private static ApplicantIdentity? AuthenticateApplicant(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        TrustRegistry registry,
        X509Certificate2? presented)
    {
        if (presented is null || registry.IsAdministratorCertificate(presented.Thumbprint) || registry.IsCertificateBlocked(presented.Thumbprint))
        {
            return null;
        }

        foreach (var pair in trustedCertificates)
        {
            if (pair.Value.Any(certificate => string.Equals(certificate.Thumbprint, presented.Thumbprint, StringComparison.OrdinalIgnoreCase)))
            {
                return registry.IsClientEnabled(pair.Key) ? new ApplicantIdentity(pair.Key, presented.Thumbprint) : null;
            }
        }

        return null;
    }

    private static async Task<ApplicantIdentity?> AuthenticateApplicantAsync(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        TrustRegistry registry,
        PostgresAuthStore? postgresStore,
        X509Certificate2? presented,
        CancellationToken cancellationToken)
    {
        if (presented is null)
        {
            return null;
        }

        if (postgresStore is not null)
        {
            if (await postgresStore.IsAdministratorCertificateAsync(presented.Thumbprint, cancellationToken) ||
                await postgresStore.IsCertificateBlockedAsync(presented.Thumbprint, cancellationToken))
            {
                return null;
            }

            var clientId = await postgresStore.FindClientByCertificateAsync(presented.Thumbprint, cancellationToken);
            if (clientId is null)
            {
                return null;
            }

            return await postgresStore.IsClientEnabledAsync(clientId, cancellationToken)
                ? new ApplicantIdentity(clientId, presented.Thumbprint)
                : null;
        }

        return AuthenticateApplicant(trustedCertificates, registry, presented);
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
        }, rollback: () =>
        {
            if (retire)
            {
                registry.UnretireCertificate(certificate.Thumbprint);
            }
            else
            {
                registry.UnrevokeCertificate(certificate.Thumbprint);
            }
        });
    }

    private static async Task<AdministrativeDecision> DecideCertificateChangeAsync(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        TrustRegistry registry,
        PostgresAuthStore? postgresStore,
        string clientId,
        string thumbprint,
        bool retire,
        CancellationToken cancellationToken)
    {
        if (postgresStore is null)
        {
            return DecideCertificateChange(trustedCertificates, registry, clientId, thumbprint, retire);
        }

        var target = new AdministrativeTarget(clientId, Subject: "certificate", Fingerprint: thumbprint);
        if (!await postgresStore.ClientExistsAsync(clientId, cancellationToken))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "client_not_found");
        }

        var owner = await postgresStore.GetCertificateClientIdAsync(thumbprint, cancellationToken);
        if (owner is null || !string.Equals(owner, clientId, StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "certificate_not_found");
        }

        var status = await postgresStore.GetCertificateStatusAsync(thumbprint, cancellationToken);
        if (string.Equals(status, "revoked", StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "already_revoked");
        }

        if (string.Equals(status, "retired", StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "already_retired");
        }

        if (await postgresStore.IsCertificateSharedWithOtherClientAsync(clientId, thumbprint, cancellationToken))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "certificate_shared");
        }

        if (retire && !await postgresStore.HasActiveAlternativeCertificateAsync(clientId, thumbprint, cancellationToken))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "replacement_required");
        }

        return Accepted(target, StatusCodes.Status200OK, () =>
        {
            if (retire)
            {
                registry.RetireCertificate(thumbprint);
            }
            else
            {
                registry.RevokeCertificate(thumbprint);
            }

            return Task.FromResult(Results.Json(new { clientId, thumbprint, status = retire ? "retired" : "revoked" }));
        }, rollback: () =>
        {
            if (retire)
            {
                registry.UnretireCertificate(thumbprint);
            }
            else
            {
                registry.UnrevokeCertificate(thumbprint);
            }
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
        }, rollback: () =>
        {
            if (retire)
            {
                registry.UnretireSigningKey(keyId);
            }
            else
            {
                registry.UnrevokeSigningKey(keyId);
            }
        });
    }

    private static async Task<AdministrativeDecision> DecideSigningKeyChangeAsync(
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        TrustRegistry registry,
        VerificationKeyStore verificationKeys,
        PostgresAuthStore? postgresStore,
        string clientId,
        string keyId,
        bool retire,
        CancellationToken cancellationToken)
    {
        if (postgresStore is null)
        {
            return DecideSigningKeyChange(trustedCertificates, registry, verificationKeys, clientId, keyId, retire);
        }

        var target = new AdministrativeTarget(clientId, Subject: "signing_key", KeyId: keyId);
        if (!await postgresStore.ClientExistsAsync(clientId, cancellationToken))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "client_not_found");
        }

        var owner = await postgresStore.GetSigningKeyClientIdAsync(keyId, cancellationToken);
        if (owner is null || !string.Equals(owner, clientId, StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(target, StatusCodes.Status404NotFound, "signing_key_not_found");
        }

        var status = await postgresStore.GetSigningKeyStatusAsync(keyId, cancellationToken);
        if (string.Equals(status, "revoked", StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "already_revoked");
        }

        if (string.Equals(status, "retired", StringComparison.OrdinalIgnoreCase))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "already_retired");
        }

        if (await postgresStore.IsSigningKeySharedWithOtherClientAsync(clientId, keyId, cancellationToken))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "signing_key_shared");
        }

        if (retire && !await postgresStore.HasActiveAlternativeSigningKeyAsync(clientId, keyId, cancellationToken))
        {
            return Rejected(target, StatusCodes.Status409Conflict, "replacement_required");
        }

        var fingerprint = await postgresStore.GetSigningKeyFingerprintAsync(keyId, cancellationToken);
        target = target with { Fingerprint = fingerprint };

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
        }, rollback: () =>
        {
            if (retire)
            {
                registry.UnretireSigningKey(keyId);
            }
            else
            {
                registry.UnrevokeSigningKey(keyId);
            }
        });
    }

    /// <summary>管理員身分只看出示的 mTLS 憑證是否登錄為管理員角色；與業務 API 的 Token 查證各自獨立。</summary>
    private static bool IsAdministrator(HttpContext httpContext, TrustRegistry registry)
    {
        var certificate = httpContext.Connection.ClientCertificate;
        return certificate is not null && registry.IsAdministratorCertificate(certificate.Thumbprint);
    }

    private static async Task<bool> IsAdministratorAsync(
        HttpContext httpContext,
        TrustRegistry registry,
        PostgresAuthStore? postgresStore,
        CancellationToken cancellationToken)
    {
        var certificate = httpContext.Connection.ClientCertificate;
        if (certificate is null)
        {
            return false;
        }

        if (postgresStore is not null)
        {
            return await postgresStore.IsAdministratorCertificateAsync(certificate.Thumbprint, cancellationToken);
        }

        return IsAdministrator(httpContext, registry);
    }

    private static IResult AdministratorRejected()
        => Results.Json(new Dictionary<string, string> { ["error"] = AdministratorAuthenticationRequired }, statusCode: StatusCodes.Status401Unauthorized);

    /// <summary>
    /// 以指定的公開憑證集合取代 Client 登錄的 JWKS（08 單）：登錄新憑證為新增一筆，退役舊憑證則自集合移除。
    /// 集合內每張憑證都能通過該 Client 的 self_signed_tls_client_auth 並取得綁定該憑證的 Token。
    /// </summary>
    public Task SetClientCertificatesAsync(string clientId, IReadOnlyList<X509Certificate2> publicCertificates, CancellationToken cancellationToken = default)
        => ReplaceTrustedCertificatesAsync(_app, _clients, _trustedCertificates, clientId, publicCertificates, cancellationToken);

    /// <summary>更新指定 Client 的信任名單與 OpenIddict JWKS；只改動該 Client 的登錄，不影響其他 Client。</summary>
    private static async Task ReplaceTrustedCertificatesAsync(
        WebApplication app,
        IReadOnlyDictionary<string, AuthServerClient> clients,
        ConcurrentDictionary<string, IReadOnlyList<X509Certificate2>> trustedCertificates,
        string clientId,
        IReadOnlyList<X509Certificate2> publicCertificates,
        CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await manager.FindByClientIdAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException($"找不到 Client {clientId}。");
        await manager.UpdateAsync(application, BuildDescriptor(clients[clientId], publicCertificates), cancellationToken);
        trustedCertificates[clientId] = publicCertificates.ToArray();
    }

    private static async Task ReplaceTrustedCertificatesWithTransactionAsync(
        WebApplication app,
        IReadOnlyDictionary<string, AuthServerClient> clients,
        string clientId,
        IReadOnlyList<X509Certificate2> publicCertificates,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SpikeDbContext>>();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.Database.SetDbConnection(transaction.Connection);
        await db.Database.UseTransactionAsync(transaction, cancellationToken);

        var scopedDb = scope.ServiceProvider.GetService<SpikeDbContext>();
        if (scopedDb is not null && !ReferenceEquals(scopedDb, db))
        {
            scopedDb.Database.SetDbConnection(transaction.Connection);
            await scopedDb.Database.UseTransactionAsync(transaction, cancellationToken);
        }

        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await manager.FindByClientIdAsync(clientId, cancellationToken)
            ?? throw new InvalidOperationException($"找不到 Client {clientId}。");
        await manager.UpdateAsync(application, BuildDescriptor(clients[clientId], publicCertificates), cancellationToken);
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

    private static async Task TryWriteAuditStandaloneAsync(PostgresAuthStore store, AdministrativeAuditRecord record, CancellationToken cancellationToken)
    {
        try
        {
            await using var conn = await store.OpenConnectionAsync(cancellationToken);
            await PostgresAuthStore.WriteAuditInternalAsync(conn, null, record, cancellationToken);
        }
        catch (Exception ex) when (ex is not AdministrativeAuditWriteFailedException && ex is not OperationCanceledException)
        {
            throw new AdministrativeAuditWriteFailedException("Failed to write standalone audit record.", ex);
        }
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
    public async Task RevokeAccessTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        using var scope = _app.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var token = await tokens.FindByReferenceIdAsync(accessToken, cancellationToken)
            ?? throw new InvalidOperationException("找不到要撤銷的 Token。");
        await tokens.TryRevokeAsync(token, cancellationToken);
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

/// <summary>管理操作稽核的對象（15 單）；未知的對象欄位為 null。Subject 為 certificate 或 signing_key。</summary>
internal sealed record AdministrativeTarget(string? ClientId, string? Subject = null, string? KeyId = null, string? Fingerprint = null);
