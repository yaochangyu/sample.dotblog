using Lab.SessionCacheProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Lab.SessionCacheProvider.Tests.Support;

public class TestServerFixture : IDisposable
{
    public HttpClient Client { get; }

    private readonly IHost _host;

    public TestServerFixture()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddHybridCache();
                    services.AddSessionCacheProvider();
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseSessionCache();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/api/session/get", (HttpContext ctx) =>
                        {
                            var key = ctx.Request.Query["key"].ToString();
                            var provider = ctx.RequestServices.GetRequiredService<SessionCacheProvider>();
                            var value = provider.Session[key];
                            return Results.Text(value?.ToString() ?? "");
                        });

                        endpoints.MapPost("/api/session/set", async (HttpContext ctx) =>
                        {
                            var form = await ctx.Request.ReadFormAsync();
                            var key = form["key"].ToString();
                            var value = form["value"].ToString();
                            var provider = ctx.RequestServices.GetRequiredService<SessionCacheProvider>();
                            provider.Session[key] = value;
                            return Results.Ok();
                        });

                        endpoints.MapGet("/api/session/get-static", (HttpContext ctx) =>
                        {
                            var key = ctx.Request.Query["key"].ToString();
                            var value = CacheSession.Current[key];
                            return Results.Text(value?.ToString() ?? "");
                        });

                        endpoints.MapPost("/api/session/set-static", async (HttpContext ctx) =>
                        {
                            var form = await ctx.Request.ReadFormAsync();
                            var key = form["key"].ToString();
                            var value = form["value"].ToString();
                            CacheSession.Current[key] = value;
                            return Results.Ok();
                        });
                    });
                });
            })
            .Start();

        Client = _host.GetTestClient();
    }

    public void Dispose()
    {
        Client.Dispose();
        _host.Dispose();
    }
}
