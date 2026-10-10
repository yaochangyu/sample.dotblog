using System.Collections.Concurrent;

namespace AuthSpike.OrdersApi;

public sealed record CreateOrderRequest(string Item, int Quantity);

public sealed record CreateOrderResponse(Guid OrderId, string ClientId);

public sealed record GetOrderResponse(Guid OrderId, string ClientId, string Item, int Quantity, string Status);

/// <summary>訂單狀態：active（有效）或 cancelled（已取消）。</summary>
public sealed record StoredOrder(string ClientId, CreateOrderRequest Request, string Status);

/// <summary>spike 用記憶體儲存；不含去重與持久化（屬後續 issue）。</summary>
public sealed class OrderStore
{
    private readonly ConcurrentDictionary<Guid, StoredOrder> _orders = new();

    /// <summary>本執行個體已建立的訂單數（測試用於確認業務副作用次數）。</summary>
    public int Count => _orders.Count;

    /// <summary>只回傳由同一已驗證 Client 建立的訂單（業務資料範圍檢查）。</summary>
    public bool TryGet(Guid orderId, string verifiedClientId, out StoredOrder? order)
    {
        if (_orders.TryGetValue(orderId, out var stored) && stored.ClientId == verifiedClientId)
        {
            order = stored;
            return true;
        }

        order = null;
        return false;
    }

    /// <summary>只能取消由同一已驗證 Client 建立的訂單；他人訂單不存在、不會被修改。</summary>
    public bool TryCancel(Guid orderId, string verifiedClientId, out StoredOrder? order)
    {
        if (!TryGet(orderId, verifiedClientId, out var current))
        {
            order = null;
            return false;
        }

        order = current! with { Status = "cancelled" };
        _orders[orderId] = order;
        return true;
    }

    /// <param name="verifiedClientId">由已驗證 Token 取得的 client_id，不得來自 Body 或外部標頭。</param>
    public Guid Add(string verifiedClientId, CreateOrderRequest request)
    {
        var orderId = Guid.NewGuid();
        _orders[orderId] = new StoredOrder(verifiedClientId, request, "active");
        return orderId;
    }
}
