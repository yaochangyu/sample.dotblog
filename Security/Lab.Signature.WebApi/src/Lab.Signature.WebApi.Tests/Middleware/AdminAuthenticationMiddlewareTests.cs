using Lab.Signature.WebApi.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Lab.Signature.WebApi.Tests.Middleware;

/// <summary>
/// 驗證 <see cref="AdminAuthenticationMiddleware"/>：非 Development 一律 404，
/// Development 下比對 X-Admin-Key，且只作用於 /api/admin/*。
/// </summary>
public class AdminAuthenticationMiddlewareTests
{
    private const string AdminApiKey = "admin-dev-only-please-change";

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Lab.Signature.WebApi";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static IConfiguration CreateConfiguration(string? adminApiKey = AdminApiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AdminApiKey"] = adminApiKey })
            .Build();

    private static DefaultHttpContext CreateContext(string path, string? adminKeyHeader = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (adminKeyHeader is not null)
        {
            context.Request.Headers["X-Admin-Key"] = adminKeyHeader;
        }

        return context;
    }

    [Fact]
    public async Task InvokeAsync_NonAdminPath_PassesThroughRegardlessOfEnvironment()
    {
        var context = CreateContext("/api/protected/orders/1");
        var nextCalled = false;
        var middleware = new AdminAuthenticationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, new FakeWebHostEnvironment("Production"), CreateConfiguration());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_AdminPathInNonDevelopmentEnvironment_Returns404WithoutCallingNext()
    {
        var context = CreateContext("/api/admin/clients", AdminApiKey);
        var nextCalled = false;
        var middleware = new AdminAuthenticationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, new FakeWebHostEnvironment("Production"), CreateConfiguration());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentWithCorrectAdminKey_CallsNext()
    {
        var context = CreateContext("/api/admin/clients", AdminApiKey);
        var nextCalled = false;
        var middleware = new AdminAuthenticationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, new FakeWebHostEnvironment("Development"), CreateConfiguration());

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentWithMissingAdminKey_Returns401WithoutCallingNext()
    {
        var context = CreateContext("/api/admin/clients", adminKeyHeader: null);
        var nextCalled = false;
        var middleware = new AdminAuthenticationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, new FakeWebHostEnvironment("Development"), CreateConfiguration());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_DevelopmentWithWrongAdminKey_Returns401WithoutCallingNext()
    {
        var context = CreateContext("/api/admin/clients", "wrong-key");
        var nextCalled = false;
        var middleware = new AdminAuthenticationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, new FakeWebHostEnvironment("Development"), CreateConfiguration());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }
}
