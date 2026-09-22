using CSharpFunctionalExtensions;
using Lab.API.Signature.Data;
using Lab.API.Signature.Models;
using Microsoft.EntityFrameworkCore;

namespace Lab.API.Signature.Repositories;

public class ApiKeyClientRepository : IApiKeyClientRepository
{
    private readonly SignatureDbContext _dbContext;

    public ApiKeyClientRepository(SignatureDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Maybe<ApiKeyClient>> FindByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var client = await _dbContext.ApiKeyClients
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.ApiKey == apiKey, cancellationToken);

        return Maybe<ApiKeyClient>.From(client);
    }

    public async Task AddAsync(ApiKeyClient client, CancellationToken cancellationToken = default)
    {
        _dbContext.ApiKeyClients.Add(client);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // SaveChangesAsync 失敗時，EF Core 不會自動把這筆 entity 從 change tracker 移除，
            // 仍會維持 Added 狀態。呼叫端（例如 Controller 重新產生 ApiKey 後在同一個 scoped
            // DbContext 上重試 AddAsync）若不先卸除它，下一次 SaveChangesAsync 會把這筆已經失敗
            // 的舊 entity 跟新 entity 一起送進同一次寫入，導致舊的衝突每次都讓重試連帶失敗。
            // 這裡在例外往上拋之前主動卸除，讓同一個 DbContext 之後的呼叫不會被這筆殘留污染。
            _dbContext.Entry(client).State = EntityState.Detached;
            throw;
        }
    }

    public async Task<IReadOnlyList<ApiKeyClient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.ApiKeyClients
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
