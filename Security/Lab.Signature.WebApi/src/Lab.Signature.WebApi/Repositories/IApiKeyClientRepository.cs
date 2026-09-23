using CSharpFunctionalExtensions;
using Lab.Signature.WebApi.Models;

namespace Lab.Signature.WebApi.Repositories;

/// <summary>
/// 依 ApiKey 查詢合作夥伴憑證。查無資料時回傳 Maybe.None，不丟例外。
/// </summary>
public interface IApiKeyClientRepository
{
    Task<Maybe<ApiKeyClient>> FindByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增一筆 Client 憑證。若 <see cref="ApiKeyClient.ApiKey"/> 已存在（主鍵衝突），
    /// 讓 <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> 往上拋，
    /// 是否重試由呼叫端（Controller/Handler 層）決定。
    /// </summary>
    Task AddAsync(ApiKeyClient client, CancellationToken cancellationToken = default);

    /// <summary>取得所有已核發的 Client 憑證（含 Secret 明碼，是否過濾由呼叫端決定）。</summary>
    Task<IReadOnlyList<ApiKeyClient>> GetAllAsync(CancellationToken cancellationToken = default);
}
