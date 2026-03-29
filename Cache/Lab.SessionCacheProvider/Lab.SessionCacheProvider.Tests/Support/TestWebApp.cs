using Lab.SessionCacheProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.SessionCacheProvider.Tests.Support;

public class TestWebApp
{
    public static WebApplication CreateApp(
        string[] args,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddHybridCache();
        builder.Services.AddSessionCacheProvider();

        configureBuilder?.Invoke(builder);

        var app = builder.Build();
        app.UseSessionCache();

        app.MapGet("/api/session/get", (HttpContext ctx) =>
        {
            var key = ctx.Request.Query["key"].ToString();
            var provider = ctx.RequestServices.GetRequiredService<SessionCacheProvider>();
            var value = provider.Session[key];
            return Results.Text(value?.ToString() ?? "");
        });

        app.MapPost("/api/session/set", async (HttpContext ctx) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var key = form["key"].ToString();
            var value = form["value"].ToString();
            var provider = ctx.RequestServices.GetRequiredService<SessionCacheProvider>();
            provider.Session[key] = value;
            return Results.Ok();
        });

        app.MapGet("/api/session/get-static", (HttpContext ctx) =>
        {
            var key = ctx.Request.Query["key"].ToString();
            var value = CacheSession.Current[key];
            return Results.Text(value?.ToString() ?? "");
        });

        app.MapPost("/api/session/set-static", async (HttpContext ctx) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var key = form["key"].ToString();
            var value = form["value"].ToString();
            CacheSession.Current[key] = value;
            return Results.Ok();
        });

        return app;
    }
}
