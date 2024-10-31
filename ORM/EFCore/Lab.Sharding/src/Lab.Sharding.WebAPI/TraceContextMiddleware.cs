using System.Security.Claims;
using Lab.Sharding.Infrastructure;
using Lab.Sharding.Infrastructure.TraceContext;

namespace Lab.Sharding.WebAPI;

public class TraceContextMiddleware
{
	private readonly RequestDelegate _next;

	public TraceContextMiddleware(RequestDelegate next)
	{
		this._next = next;
	}

	public async Task Invoke(HttpContext httpContext, ILogger<TraceContextMiddleware> logger)
	{
		var traceId = httpContext.Request.Headers[SysHeaderNames.TraceId].FirstOrDefault();

		//// 若調用端沒有傳入 traceId，則產生一個新的 traceId
		if (string.IsNullOrWhiteSpace(traceId))
		{
			traceId = httpContext.TraceIdentifier;
		}

		// 模擬登入
		Signin(httpContext);

		if (httpContext.User.Identity.IsAuthenticated == false)
		{
			httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
			await httpContext.Response.WriteAsJsonAsync(new Failure
			{
				Code = nameof(FailureCode.Unauthorized),
				Message = "not login"
			});
			return;
		}

		var userId = httpContext.User.Identity.Name;

		// 寫入 trace context 到 object context setter
		var contextSetter = httpContext.RequestServices.GetService<IContextSetter<TraceContext>>();
		contextSetter.Set(new TraceContext { TraceId = traceId, UserId = userId });

		// 附加 traceId 與 userId 到 log 中
		using var _ = logger.BeginScope("{Location},{TraceId},{UserId}",
										"TW", traceId, userId);

		// 附加 traceId 到 response header 中
		IContextGetter<TraceContext?>? contextGetter =
			httpContext.RequestServices.GetService<IContextGetter<TraceContext>>();
		var traceContext = contextGetter.Get();
		httpContext.Response.Headers.TryAdd(SysHeaderNames.TraceId, traceContext.TraceId);

		await this._next.Invoke(httpContext);
	}

	/// <summary>
	///     假的登入
	/// </summary>
	/// <param name="context"></param>
	private static void Signin(HttpContext context)
	{
		var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "yao"), new Claim(ClaimTypes.Name, "yao") };
		var identity = new ClaimsIdentity(claims, "Bearer");
		var principal = new ClaimsPrincipal(identity);
		context.User = principal;
	}
}