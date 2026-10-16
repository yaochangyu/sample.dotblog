using System.Security.Cryptography.X509Certificates;
using System.Threading;
using AuthSpike.Audit;
using AuthSpike.Hosting;
using AuthSpike.Replay;
using AuthSpike.Signing;
using AuthSpike.Trust;
using AuthSpike.Verification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthSpike.OrdersApi;

/// <summary>
/// 建立訂單 API（API First：契約見 doc/openapi.yml）。
/// 呼叫者查證：Token 由授權伺服器 introspection 查證（含 mTLS cnf 綁定），並經 CallerVerifier 快取與撤銷狀態檢查；
/// 查證服務無法連線且無有效快取時回應 503，不偽裝成 401。
/// </summary>
public sealed class OrdersApiHost : IAsyncDisposable
{
    private const string ReadScope = "orders.read";
    private const string WriteScope = "orders.write";

    /// <summary>使用者專屬的讀取（04 單）：呼叫者類型必須為使用者且持有 orders.read；服務即使持有該 scope 也被拒絕。</summary>
    private const string UserOnlyReadPolicy = "user-only:orders.read";

    /// <summary>使用者寫入的防重放標頭（05 單）：使用者寫入沒有請求簽章，改由此 nonce 防重放；lab 暫定，待使用者確認。</summary>
    private const string UserRequestNonceHeader = "X-Request-Nonce";
    private const int MaxUserRequestNonceLength = 128;

    /// <summary>lab 暫定：使用者寫入 nonce 的保存期（待使用者確認）。</summary>
    private static readonly TimeSpan UserRequestNonceRetention = TimeSpan.FromHours(24);

    private readonly WebApplication _app;
    private int _created;

    private OrdersApiHost(WebApplication app, int port)
    {
        _app = app;
        Port = port;
    }

    public int Port { get; }

    /// <summary>最近一次業務請求的來源埠（用於確認既有 TCP 連線是否被重用）。</summary>
    public int LastRemotePort => _callers?.LastRemotePort ?? 0;

    /// <summary>本執行個體已建立的訂單數（業務副作用次數；多個執行個體共用同一份訂單資料）。</summary>
    public int OrderCount => Volatile.Read(ref _created);

    private CallerVerifier? _callers;
    private UserTokenIntrospector? _userIntrospector;

    public static async Task<OrdersApiHost> StartAsync(
        int port,
        SpikeTrust trust,
        X509Certificate2 serverCertificate,
        Uri authorizationServer,
        string resourceClientId,
        X509Certificate2 resourceClientCertificate,
        VerificationKeyStore signatureKeys,
        INonceReplayStore replayStore,
        TrustRegistry registry,
        TimeSpan verificationCacheLifetime,
        OrderStore orders,
        SecurityAuditLog audit)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.ListenLocalhost(port, listen => listen.UseHttps(https =>
        {
            https.ServerCertificate = serverCertificate;
            // 必須收到 TLS 用戶端憑證，才能讓 validation 堆疊比對 cnf；是否信任由 OpenIddict 判斷。
            https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
            https.ClientCertificateValidation = static (_, _, _) => true;
        })));

        builder.Services.AddSingleton(orders);
        builder.Services.AddSingleton(audit);
        builder.Services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(authorizationServer);
                options.SetClientId(resourceClientId);

                // 只接受目標 API 為本服務的 Token；其他 API 的 Token 即使有效也被拒絕。
                options.AddAudiences(resourceClientId);

                // 以此憑證作為 introspection 的 mTLS 用戶端憑證（OpenIddict 文件要求的用法）。
                options.AddSigningCertificate(resourceClientCertificate);
                options.UseIntrospection();

                options.UseSystemNetHttp(http => http.ConfigureHttpClientHandler(handler =>
                    handler.ServerCertificateCustomValidationCallback = trust.ServerCertificateValidator));

                options.UseAspNetCore();
            });

        // 刻意不設定 DefaultAuthenticateScheme：否則 ASP.NET Core 的驗證中介軟體會對每個請求自動向授權伺服器 introspection，
        // 使 CallerVerifier 的查證快取失效，並在授權伺服器停止時讓每個請求先等完 OpenIddict 的重試退避（約 15 秒）。
        // introspection 只由 CallerVerifier 在快取未命中時明確觸發（指定 scheme）。
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        // scope 白名單：Token 有效但缺少該操作所需的 scope 時回應 403（業務資料範圍另於 handler 檢查）。
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(ReadScope, policy => policy.RequireAssertion(context => HasPermission(context, ReadScope)))
            .AddPolicy(WriteScope, policy => policy.RequireAssertion(context => HasPermission(context, WriteScope)))
            // 呼叫者類型規則（04 單）：限定使用者的操作只接受使用者呼叫者，服務的 scope 不能代替使用者的限制。
            .AddPolicy(UserOnlyReadPolicy, policy => policy.RequireAssertion(context => HasCallerPermission(context, CallerType.User, ReadScope)));

        var app = builder.Build();
        var instance = new OrdersApiHost(app, port);
        instance._userIntrospector = new UserTokenIntrospector(authorizationServer, resourceClientCertificate, trust);
        instance._callers = new CallerVerifier(registry, verificationCacheLifetime, instance._userIntrospector);
        var callers = instance._callers;

        // 稽核信封：回應帶 X-Correlation-Id；若請求未經任何決策點（例如 scope 不足被授權中介軟體拒絕），回應後補記 denied。
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/orders"))
            {
                await next();
                return;
            }

            context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier;

            // 回應開始送出時狀態碼已定案：先補記最終業務結果，呼叫端收到回應時紀錄已完整。
            context.Response.OnStarting(() =>
            {
                audit.CompleteResult(context.TraceIdentifier, context.Response.StatusCode);
                return Task.CompletedTask;
            });
            await next();

            if (!CallerAudit.IsDecided(context)
                && !CallerAudit.TryRecord(context, audit, "denied", $"status_{context.Response.StatusCode}", claimedCaller: ClaimedCallerOf(context))
                && !context.Response.HasStarted)
            {
                await WriteAuditUnavailableAsync(context);
            }
        });

        // 呼叫者查證先於授權與簽章：未通過查證的請求不進入 scope 檢查與業務處理。
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/orders"))
            {
                await next();
                return;
            }

            var (outcome, principal) = await callers.VerifyAsync(context);
            if (outcome == CallerOutcome.Verified)
            {
                context.User = principal!;
                var caller = UserCallers.IsUserPrincipal(principal!)
                    ? VerifiedCaller.FromUserPrincipal(principal!)
                    : VerifiedCaller.FromServicePrincipal(principal!);
                VerifiedCaller.Attach(context, caller);
                await next();
                return;
            }

            if (outcome == CallerOutcome.Unavailable)
            {
                // 查證服務暫時無法連線且無有效快取：明確回報服務錯誤，不放行、不進入業務副作用。
                if (!await RecordOrAuditUnavailableAsync(context, audit, "unavailable", "caller_verification_unavailable"))
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { error = "verification_unavailable" });
                return;
            }

            // 認證失敗：principal（若有）僅為宣稱身分，標為未驗證。
            if (!await RecordOrAuditUnavailableAsync(context, audit, "rejected", "caller_rejected", claimedCaller: ClaimedCaller.FromPrincipal(principal)))
            {
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        });

        app.UseAuthorization();

        // 業務呼叫需同時通過 Token 查證（已於上方閘門處理）與請求簽章、防重放驗證。
        var verifier = new SignedRequestVerifier(signatureKeys, replayStore, registry);
        app.Use(async (context, next) =>
        {
            var caller = VerifiedCaller.TryGet(context);
            if (caller is null || !context.Request.Path.StartsWithSegments("/orders"))
            {
                await next();
                return;
            }

            if (caller.Type == CallerType.User)
            {
                // 使用者寫入（05 單）：沒有請求簽章，改以 X-Request-Nonce 防重放；nonce 依使用者業務範圍分開保存，讀取不需要防重放。
                if (!HttpMethods.IsGet(context.Request.Method))
                {
                    var nonce = context.Request.Headers[UserRequestNonceHeader].ToString();
                    if (nonce.Length == 0 || nonce.Length > MaxUserRequestNonceLength)
                    {
                        if (!await RecordOrAuditUnavailableAsync(context, audit, "rejected", "user_request_nonce_missing", claimedCaller: ClaimedCaller.FromVerified(caller)))
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    ReplayRegistration registration;
                    try
                    {
                        var now = DateTimeOffset.UtcNow;
                        registration = await replayStore.TryRegisterAsync(caller.BusinessScope, nonce, now, now + UserRequestNonceRetention);
                    }
                    catch (ReplayStateUnavailableException)
                    {
                        // 防重放狀態無法可靠讀寫：明確回報服務錯誤，不放行、不進入業務副作用。
                        if (!await RecordOrAuditUnavailableAsync(context, audit, "unavailable", "replay_state_unavailable", claimedCaller: ClaimedCaller.FromVerified(caller)))
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        await context.Response.WriteAsJsonAsync(new { error = "replay_state_unavailable" });
                        return;
                    }

                    if (registration != ReplayRegistration.Registered)
                    {
                        var userReason = registration == ReplayRegistration.Replayed ? "user_request_replayed" : "user_request_before_replay_epoch";
                        if (!await RecordOrAuditUnavailableAsync(context, audit, "rejected", userReason, claimedCaller: ClaimedCaller.FromVerified(caller)))
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                }

                // 紀錄先於業務處理寫入；寫入失敗不進入業務處理。
                if (!await RecordOrAuditUnavailableAsync(context, audit, "accepted", "user_token_verified", verifiedCaller: caller))
                {
                    return;
                }

                await next();
                return;
            }

            var clientId = caller.Identity;
            var signature = await verifier.VerifyAsync(context, clientId);
            var outcome = signature.Outcome;
            if (outcome != SignatureOutcome.Accepted)
            {
                // 簽章或防重放未通過：撤銷已附加的已驗證呼叫者，業務處理不會看到它；後續紀錄只把宣稱的身分標為未驗證。
                VerifiedCaller.Detach(context);
            }

            if (outcome == SignatureOutcome.Accepted)
            {
                // 紀錄先於業務處理寫入；寫入失敗則不進入業務副作用。最終業務結果於回應送出時補記（見上方稽核信封）。
                if (!await RecordOrAuditUnavailableAsync(context, audit, "accepted", "signature_verified", verifiedCaller: caller, signatureKeyId: signature.KeyId))
                {
                    return;
                }

                await next();
                return;
            }

            if (outcome == SignatureOutcome.ReplayStateUnavailable)
            {
                // 防重放狀態無法可靠讀寫：明確回報服務錯誤，不放行、不進入業務副作用。
                if (!await RecordOrAuditUnavailableAsync(context, audit, "unavailable", "replay_state_unavailable", claimedCaller: ClaimedCaller.FromVerified(caller)))
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new { error = "replay_state_unavailable" });
                return;
            }

            // 驗簽失敗、重放或早於防重放紀元：請求宣稱的 Client 標為未驗證。
            var reason = outcome switch
            {
                SignatureOutcome.Replayed => "signature_replayed",
                SignatureOutcome.BeforeReplayEpoch => "signature_before_replay_epoch",
                _ => "signature_rejected",
            };
            if (!await RecordOrAuditUnavailableAsync(context, audit, "rejected", reason, claimedCaller: ClaimedCaller.FromVerified(caller)))
            {
                return;
            }

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        });

        // 建立訂單（06 單）：穩定業務識別 orderReference 決定業務操作是否相同；Idempotency-Key 只定位請求嘗試。
        app.MapPost("/orders", async (CreateOrderRequest request, OrderStore store, HttpContext httpContext) =>
            {
                // 已驗證呼叫者取自 introspection 結果；Body 與外部標頭宣稱的身分一律忽略。
                var caller = VerifiedCaller.Get(httpContext);
                var clientId = caller.Identity;
                if (string.IsNullOrWhiteSpace(request.OrderReference) || request.OrderReference.Length > 64)
                {
                    return Results.BadRequest(new { error = "order_reference_invalid" });
                }

                var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();
                if (string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    return Results.BadRequest(new { error = "idempotency_key_required" });
                }

                var result = await store.CreateOnceAsync(caller.BusinessScope, idempotencyKey, request, DateTimeOffset.UtcNow);
                switch (result.Outcome)
                {
                    case CreateOutcome.Created:
                        Interlocked.Increment(ref instance._created);
                        return Results.Created($"/orders/{result.OrderId}", new CreateOrderResponse(result.OrderId!.Value, clientId));
                    case CreateOutcome.Replayed:
                        httpContext.Response.Headers["Idempotent-Replayed"] = "true";
                        return Results.Created($"/orders/{result.OrderId}", new CreateOrderResponse(result.OrderId!.Value, clientId));
                    case CreateOutcome.InProgress:
                        return Results.Json(new { error = "operation_in_progress", status = "processing" }, statusCode: StatusCodes.Status409Conflict);
                    case CreateOutcome.KeyConflict:
                        return Results.UnprocessableEntity(new { error = "idempotency_key_conflict" });
                    case CreateOutcome.IdentityConflict:
                        return Results.UnprocessableEntity(new { error = "order_reference_conflict" });
                    default:
                        return Results.Json(new { error = "business_commit_unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
                }
            })
            .RequireAuthorization(WriteScope);

        // 無 Body 的查詢：只回傳已驗證 Client 自己建立的訂單，其他 Client 的訂單以 404 處理（不洩漏存在與否）。
        app.MapGet("/orders/{orderId:guid}", (Guid orderId, OrderStore store, HttpContext httpContext) =>
            {
                var caller = VerifiedCaller.Get(httpContext);
                var clientId = caller.Identity;
                return store.TryGet(orderId, caller.BusinessScope, out var order)
                    ? Results.Ok(ToResponse(orderId, clientId, order!))
                    : Results.NotFound();
            })
            .RequireAuthorization(ReadScope);

        // 使用者專屬查詢（04 單）：只回傳使用者呼叫者自己的訂單；服務呼叫者由呼叫者類型規則直接以 403 拒絕，不進入資料查詢。
        app.MapGet("/orders/me/{orderId:guid}", (Guid orderId, OrderStore store, HttpContext httpContext) =>
            {
                var caller = VerifiedCaller.Get(httpContext);
                var clientId = caller.Identity;
                return store.TryGet(orderId, caller.BusinessScope, out var order)
                    ? Results.Ok(ToResponse(orderId, clientId, order!))
                    : Results.NotFound();
            })
            .RequireAuthorization(UserOnlyReadPolicy);

        // 修改：取消訂單；同樣只能取消已驗證 Client 自己建立的訂單，他人訂單不會被修改。
        app.MapPost("/orders/{orderId:guid}/cancel", (Guid orderId, OrderStore store, HttpContext httpContext) =>
            {
                var caller = VerifiedCaller.Get(httpContext);
                var clientId = caller.Identity;
                return store.TryCancel(orderId, caller.BusinessScope, out var order)
                    ? Results.Ok(ToResponse(orderId, clientId, order!))
                    : Results.NotFound();
            })
            .RequireAuthorization(WriteScope);

        await app.StartAsync();
        return instance;
    }

    /// <summary>授權規則只依已驗證呼叫者的權限判斷（不直接讀取 Token 的 scope claim）。</summary>
    private static bool HasPermission(AuthorizationHandlerContext context, string permission)
        => context.Resource is HttpContext httpContext && VerifiedCaller.TryGet(httpContext)?.HasPermission(permission) == true;

    /// <summary>呼叫者類型與權限同時成立才授權（04 單）；呼叫者類型由已驗證的 Token 決定，不採信請求宣稱。</summary>
    private static bool HasCallerPermission(AuthorizationHandlerContext context, CallerType type, string permission)
        => context.Resource is HttpContext httpContext
            && VerifiedCaller.TryGet(httpContext) is { } caller
            && caller.Type == type
            && caller.HasPermission(permission);

    /// <summary>寫入稽核紀錄；寫入失敗時以 503 audit_unavailable 明確回報並回傳 false，呼叫端不得進入業務處理。</summary>
    private static async Task<bool> RecordOrAuditUnavailableAsync(
        HttpContext context,
        SecurityAuditLog audit,
        string outcome,
        string reason,
        VerifiedCaller? verifiedCaller = null,
        string? signatureKeyId = null,
        ClaimedCaller? claimedCaller = null)
    {
        if (CallerAudit.TryRecord(context, audit, outcome, reason, verifiedCaller, signatureKeyId, claimedCaller))
        {
            return true;
        }

        await WriteAuditUnavailableAsync(context);
        return false;
    }

    /// <summary>稽核紀錄無法寫入：以 503 明確回報，不得放行、不得假報成功。</summary>
    private static Task WriteAuditUnavailableAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return context.Response.WriteAsJsonAsync(new { error = "audit_unavailable" });
    }

    /// <summary>授權階段（例如 scope 或呼叫者類型規則）拒絕的請求：context.User 只是宣稱的身分，標為未驗證。</summary>
    private static ClaimedCaller? ClaimedCallerOf(HttpContext context)
        => context.User.Identity?.IsAuthenticated == true ? ClaimedCaller.FromPrincipal(context.User) : null;

    private static GetOrderResponse ToResponse(Guid orderId, string clientId, StoredOrder order)
        => new(orderId, clientId, order.Request.Item, order.Request.Quantity, order.Status);

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        _userIntrospector?.Dispose();
    }
}
