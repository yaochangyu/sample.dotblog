using Lab.Signature.WebApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using Testcontainers.PostgreSql;

namespace Lab.Signature.WebApi.Tests.Support;

/// <summary>
/// 整個測試 assembly 只啟動一次真實 PostgreSQL 容器與一個 <see cref="CustomWebApplicationFactory"/>，
/// 所有 Scenario 共用，避免每個情境都重新啟動容器導致測試跑得極慢。
/// 各 Scenario 之間用各自獨立產生的 ApiKey（GUID 前綴）避免互相污染資料。
/// </summary>
[Binding]
public static class TestRunHooks
{
    private static PostgreSqlContainer? _postgresContainer;

    public static CustomWebApplicationFactory Factory { get; private set; } = null!;

    [BeforeTestRun]
    public static async Task BeforeTestRunAsync()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("lab_api_signature_bdd")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _postgresContainer.StartAsync();

        Factory = new CustomWebApplicationFactory(_postgresContainer.GetConnectionString());

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SignatureDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRunAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }
}
