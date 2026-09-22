using Lab.API.Signature.Data;
using Lab.API.Signature.Models;
using Lab.API.Signature.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Lab.API.Signature.Tests.Repositories;

public class ApiKeyClientRepositoryTests
{
    private static SignatureDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<SignatureDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new SignatureDbContext(options);
    }

    [Fact]
    public async Task AddAsync_NewClient_CanBeFoundAfterward()
    {
        await using var dbContext = CreateDbContext(Guid.NewGuid().ToString("N"));
        var repository = new ApiKeyClientRepository(dbContext);
        var client = new ApiKeyClient
        {
            ApiKey = "key-test-001",
            Secret = "secret-test-001",
            ClientName = "Test Client",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await repository.AddAsync(client);

        var found = await repository.FindByApiKeyAsync("key-test-001");
        Assert.True(found.HasValue);
        Assert.Equal("Test Client", found.Value.ClientName);
        Assert.Equal("secret-test-001", found.Value.Secret);
    }

    [Fact]
    public async Task AddAsync_DuplicateApiKeyAcrossSeparateContexts_PropagatesExceptionWithoutSwallowingIt()
    {
        // 完成條件驗證的是「AddAsync 本身不吃掉衝突例外，直接往上拋」，不是特定例外型別：
        // 真實 Npgsql 對主鍵衝突會丟 DbUpdateException，但 EF Core InMemory provider（測試替身）
        // 對主鍵衝突是在 SaveChangesAsync 內直接丟 ArgumentException，兩者型別不同但語意一致
        // （呼叫端都能觀察到例外並決定是否重試），這裡照 InMemory provider 的實際行為斷言。
        var databaseName = Guid.NewGuid().ToString("N");

        await using (var firstContext = CreateDbContext(databaseName))
        {
            var firstRepository = new ApiKeyClientRepository(firstContext);
            await firstRepository.AddAsync(new ApiKeyClient
            {
                ApiKey = "key-dup",
                Secret = "secret-1",
                ClientName = "A",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await using var secondContext = CreateDbContext(databaseName);
        var secondRepository = new ApiKeyClientRepository(secondContext);
        var duplicate = new ApiKeyClient
        {
            ApiKey = "key-dup",
            Secret = "secret-2",
            ClientName = "B",
            CreatedAt = DateTimeOffset.UtcNow
        };

        await Assert.ThrowsAsync<ArgumentException>(() => secondRepository.AddAsync(duplicate));
    }

    [Fact]
    public async Task AddAsync_AfterFailedSaveDueToConflict_DetachesFailedEntitySoNextCallOnSameContextSucceeds()
    {
        // 對應計畫書 AdminClientsController 的重試設計：同一個 request-scoped DbContext 在同一次
        // HTTP 請求內被重複呼叫 AddAsync。這裡驗證第一次因為衝突而失敗後，AddAsync 有把那筆失敗的
        // entity 從 change tracker 卸除，讓「同一個」Repository/DbContext 之後還能成功新增另一筆
        // 不衝突的 Client——如果沒有卸除，失敗的舊 entity 會殘留在 tracker 裡，跟著下一次
        // SaveChangesAsync 一起送出，導致重試永遠因為同一個舊衝突失敗。
        var databaseName = Guid.NewGuid().ToString("N");

        await using (var seedContext = CreateDbContext(databaseName))
        {
            var seedRepository = new ApiKeyClientRepository(seedContext);
            await seedRepository.AddAsync(new ApiKeyClient
            {
                ApiKey = "key-dup",
                Secret = "secret-existing",
                ClientName = "Existing",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await using var requestScopedContext = CreateDbContext(databaseName);
        var repository = new ApiKeyClientRepository(requestScopedContext);

        var conflicting = new ApiKeyClient
        {
            ApiKey = "key-dup",
            Secret = "secret-retry-1",
            ClientName = "Retry Attempt 1",
            CreatedAt = DateTimeOffset.UtcNow
        };
        await Assert.ThrowsAsync<ArgumentException>(() => repository.AddAsync(conflicting));

        var unique = new ApiKeyClient
        {
            ApiKey = "key-unique",
            Secret = "secret-retry-2",
            ClientName = "Retry Attempt 2",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 沒有卸除失敗 entity 的話，這一行也會因為殘留的 "key-dup" 衝突而丟例外。
        await repository.AddAsync(unique);

        var found = await repository.FindByApiKeyAsync("key-unique");
        Assert.True(found.HasValue);
        Assert.Equal("Retry Attempt 2", found.Value.ClientName);
    }

    [Fact]
    public async Task GetAllAsync_MultipleClients_ReturnsAllOfThem()
    {
        await using var dbContext = CreateDbContext(Guid.NewGuid().ToString("N"));
        var repository = new ApiKeyClientRepository(dbContext);
        await repository.AddAsync(new ApiKeyClient
        {
            ApiKey = "key-1", Secret = "s1", ClientName = "A", CreatedAt = DateTimeOffset.UtcNow
        });
        await repository.AddAsync(new ApiKeyClient
        {
            ApiKey = "key-2", Secret = "s2", ClientName = "B", CreatedAt = DateTimeOffset.UtcNow
        });

        var all = await repository.GetAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Contains(all, c => c.ApiKey == "key-1");
        Assert.Contains(all, c => c.ApiKey == "key-2");
    }

    [Fact]
    public async Task GetAllAsync_NoClients_ReturnsEmptyList()
    {
        await using var dbContext = CreateDbContext(Guid.NewGuid().ToString("N"));
        var repository = new ApiKeyClientRepository(dbContext);

        var all = await repository.GetAllAsync();

        Assert.Empty(all);
    }
}
