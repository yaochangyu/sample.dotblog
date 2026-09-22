using CSharpFunctionalExtensions;
using Lab.API.Signature.Models;

namespace Lab.API.Signature.Repositories;

/// <summary>
/// 依 ApiKey 查詢合作夥伴憑證。查無資料時回傳 Maybe.None，不丟例外。
/// </summary>
public interface IApiKeyClientRepository
{
    Task<Maybe<ApiKeyClient>> FindByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}
