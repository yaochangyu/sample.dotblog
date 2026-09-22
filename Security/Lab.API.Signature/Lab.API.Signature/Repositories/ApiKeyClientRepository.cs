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
}
