using Microsoft.AspNetCore.Mvc.Filters;

namespace IdempotencyKey.WebApi.IdempotencyKeys;

/// <summary>
/// 套用 Idempotency Key 保護的 Action Filter。
/// 使用 IFilterFactory 支援 DI，可直接標註於 Controller action 上。
/// </summary>
/// <example>
/// [HttpPost]
/// [IdempotencyKey]
/// public async Task&lt;ActionResult&lt;Member&gt;&gt; Create(CreateMemberRequest request) { ... }
///
/// // 自訂 TTL（小時）與是否強制要求 header
/// [IdempotencyKey(TtlHours = 48, Required = false)]
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IdempotencyKeyAttribute : Attribute, IFilterFactory
{
    /// <summary>Idempotency key 的保留時間（小時），預設 24 小時。</summary>
    public int TtlHours { get; set; } = 24;

    /// <summary>若為 true，缺少 Idempotency-Key header 時回傳 400；否則略過冪等保護。預設 true。</summary>
    public bool Required { get; set; } = true;

    // 每個請求建立獨立的 filter 實例，確保 DbContext（Scoped）正確注入
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var filter = ActivatorUtilities.CreateInstance<IdempotencyKeyFilter>(serviceProvider);
        filter.TtlHours = TtlHours;
        filter.Required = Required;
        return filter;
    }
}
