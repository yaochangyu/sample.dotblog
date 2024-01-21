using Microsoft.AspNetCore.Mvc;

namespace WebAppB.Controllers;

[ApiController]
public class Demo2Controller : ControllerBase
{
    private readonly ILogger<Demo2Controller> _logger;

    public Demo2Controller(ILogger<Demo2Controller> logger)
    {
        this._logger = logger;
    }

    static long s_counter = 1;
    static object s_lock = new();
    static Random s_random = new();

    [HttpGet]
    [Route("/api/demo2")]
    public async Task<ActionResult> Get()
    {
        var requestId = this.HttpContext.TraceIdentifier;

        var cookies = this.ShouldWithoutCookies();
        var headers = this.ShouldWithoutHeaders();
        if (cookies.Any()
            || headers.Any())
        {
            return this.Ok(new
            {
                Headers = headers.Any() ? headers : null,
                Cookies = cookies.Any() ? cookies : null,
            });
        }

        if (this.Request.Headers.TryGetValue("A", out var context))
        {
            var identifier = this.HttpContext.TraceIdentifier;
            var data = context.FirstOrDefault();

            //取得 Request Id
            this._logger.LogInformation("1.Id={@RequestId}, At 'Demo2', Receive request headers[A] ={@Data}",
                identifier,
                data);

            this.Response.Headers.Add("B", data);
            this.Response.Cookies.Append("B", data);

            //每一個請求都會有一個獨立的自動增量數字
            lock (s_lock)
            {
                s_counter++;
                this.Response.Headers.Add("C", s_counter.ToString());
                this.Response.Cookies.Append("C", s_counter.ToString());
            }

            this.Response.Headers.Add("D", s_random.NextInt64(1000).ToString());
            this.Response.Cookies.Append("D", s_random.NextInt64(1000).ToString());
        }

        return this.NoContent();
    }

    private Dictionary<string, string> ShouldWithoutHeaders()
    {
        var identifier = this.HttpContext.TraceIdentifier;
        var result = new Dictionary<string, string>();
        if (this.Request.Headers.TryGetValue("B", out var b))
        {
            this._logger.LogError("1.Id={RequestId}, At 'Demo2', Receive request headers[B] ={Data}",
                identifier,
                b);
            result.Add("B", b);
        }

        if (this.Request.Headers.TryGetValue("C", out var c))
        {
            this._logger.LogError("2.Id={RequestId}, At 'Demo2', Receive request headers[C] ={Data}",
                identifier,
                c);
            result.Add("C", c);
        }

        if (this.Request.Headers.TryGetValue("D", out var d))
        {
            this._logger.LogError("3.Id={RequestId}, At 'Demo2', Receive request headers[D] ={Data}",
                identifier,
                d);
            result.Add("D", d);
        }

        return result;
    }

    private Dictionary<string, string> ShouldWithoutCookies()
    {
        var result = new Dictionary<string, string>();
        var identifier = this.HttpContext.TraceIdentifier;
        if (this.Request.Cookies.TryGetValue("B", out var b))
        {
            this._logger.LogError("1.Id={RequestId}, At 'Demo2', Receive request cookies[B] ={Data}",
                identifier,
                b);
            result.Add("B", b);
        }

        if (this.Request.Cookies.TryGetValue("C", out var c))
        {
            this._logger.LogError("2.Id={RequestId}, At 'Demo2', Receive request cookies[C] ={Data}",
                identifier,
                c);
            result.Add("C", c);
        }

        if (this.Request.Cookies.TryGetValue("D", out var d))
        {
            this._logger.LogError("3.Id={RequestId}, At 'Demo2', Receive request cookies[D] ={Data}",
                identifier,
                d);
            result.Add("D", d);
        }

        if (this.Request.Cookies.TryGetValue("E", out var e))
        {
            this._logger.LogError("4.Id={RequestId}, At 'Demo2', Receive request cookies[E] ={Data}",
                identifier,
                e);
            result.Add("E", e);
        }

        if (this.Request.Cookies.TryGetValue("5.F", out var f))
        {
            this._logger.LogError("Id={RequestId}, At 'Demo2', Receive request cookies[F] ={Data}",
                identifier,
                f);
            result.Add("F", f);
        }

        return result;
    }
}