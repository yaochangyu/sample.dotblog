using System.Security.Cryptography.X509Certificates;
using System.Threading;
using AuthSpike.Hosting;
using AuthSpike.Replay;
using AuthSpike.Signing;
using AuthSpike.Trust;
using AuthSpike.Verification;
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

    public static async Task<OrdersApiHost> StartAsync(
        int port,
        SpikeTrust trust,
        X509Certificate2 serverCertificate,
        Uri authorizationServer,
        string resourceClientId,
        X509Certificate2 resourceClientCertificate,
        IReadOnlyDictionary<string, SignatureKey> signatureKeys,
        NonceReplayStore replayStore,
        TrustRegistry registry,
        TimeSpan verificationCacheLifetime,
        OrderStore orders)
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

        builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

        // scope 白名單：Token 有效但缺少該操作所需的 scope 時回應 403（業務資料範圍另於 handler 檢查）。
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(ReadScope, policy => policy.RequireAssertion(context => context.User.HasScope(ReadScope)))
            .AddPolicy(WriteScope, policy => policy.RequireAssertion(context => context.User.HasScope(WriteScope)));

        var app = builder.Build();
        var instance = new OrdersApiHost(app, port);
        instance._callers = new CallerVerifier(registry, verificationCacheLifetime);
        var callers = instance._callers;

        // 呼叫者查證先於授權與簽章：未通過查證的請求不進入 scope 檢查與業務處理。
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/orders"))
            {
                await next();
                return;
            }

            var (outcome, principal) = await callers.VerifyAsync(context);
            switch (outcome)
            {
                case CallerOutcome.Verified:
                    context.User = principal!;
                    await next();
                    return;
                case CallerOutcome.Unavailable:
                    // 查證服務暫時無法連線且無有效快取：明確回報服務錯誤，不放行、不進入業務副作用。
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(new { error = "verification_unavailable" });
                    return;
                default:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
            }
        });

        app.UseAuthorization();

        // 業務呼叫需同時通過 Token 查證（已於上方閘門處理）與請求簽章、防重放驗證。
        var verifier = new SignedRequestVerifier(signatureKeys, replayStore, registry);
        app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated != true || !context.Request.Path.StartsWithSegments("/orders"))
            {
                await next();
                return;
            }

            var outcome = await verifier.VerifyAsync(context, context.User.GetClaim(Claims.ClientId) ?? string.Empty);
            switch (outcome)
            {
                case SignatureOutcome.Accepted:
                    await next();
                    return;
                case SignatureOutcome.ReplayStateUnavailable:
                    // 防重放狀態無法可靠讀寫：明確回報服務錯誤，不放行、不進入業務副作用。
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(new { error = "replay_state_unavailable" });
                    return;
                default:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
            }
        });

        app.MapPost("/orders", (CreateOrderRequest request, OrderStore store, HttpContext httpContext) =>
            {
                // 已驗證呼叫者取自 introspection 結果；Body 與外部標頭宣稱的身分一律忽略。
                var clientId = httpContext.User.GetClaim(Claims.ClientId)
                    ?? throw new InvalidOperationException("Verified client_id is missing.");
                var orderId = store.Add(clientId, request);
                Interlocked.Increment(ref instance._created);
                return Results.Created($"/orders/{orderId}", new CreateOrderResponse(orderId, clientId));
            })
            .RequireAuthorization(WriteScope);

        // 無 Body 的查詢：只回傳已驗證 Client 自己建立的訂單，其他 Client 的訂單以 404 處理（不洩漏存在與否）。
        app.MapGet("/orders/{orderId:guid}", (Guid orderId, OrderStore store, HttpContext httpContext) =>
            {
                var clientId = httpContext.User.GetClaim(Claims.ClientId)
                    ?? throw new InvalidOperationException("Verified client_id is missing.");
                return store.TryGet(orderId, clientId, out var order)
                    ? Results.Ok(ToResponse(orderId, clientId, order!))
                    : Results.NotFound();
            })
            .RequireAuthorization(ReadScope);

        // 修改：取消訂單；同樣只能取消已驗證 Client 自己建立的訂單，他人訂單不會被修改。
        app.MapPost("/orders/{orderId:guid}/cancel", (Guid orderId, OrderStore store, HttpContext httpContext) =>
            {
                var clientId = httpContext.User.GetClaim(Claims.ClientId)
                    ?? throw new InvalidOperationException("Verified client_id is missing.");
                return store.TryCancel(orderId, clientId, out var order)
                    ? Results.Ok(ToResponse(orderId, clientId, order!))
                    : Results.NotFound();
            })
            .RequireAuthorization(WriteScope);

        await app.StartAsync();
        return instance;
    }

    private static GetOrderResponse ToResponse(Guid orderId, string clientId, StoredOrder order)
        => new(orderId, clientId, order.Request.Item, order.Request.Quantity, order.Status);

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
