namespace Lab.SerilogProject.WebApi;

public class TraceMiddleware
{
    private readonly RequestDelegate _next;

    public TraceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ILogger<TraceMiddleware> logger)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["UserId"] = "svrooij",
            ["OperationType"] = "update",
        }))
        {
            await this._next.Invoke(context);
        }
    
        // using (logger.BeginScope("{_rid}", Guid.NewGuid()))
        // {
        //     await this._next.Invoke(context);
        // }
    }
}
