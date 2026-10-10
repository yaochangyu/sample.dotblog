using System.Text.Json;

namespace AuthSpike.OrdersApi;

/// <summary>建立訂單請求；OrderReference 為穩定業務識別（client_id 範圍內唯一），重試時沿用同一值（06 單）。</summary>
public sealed record CreateOrderRequest(string OrderReference, string Item, int Quantity);

public sealed record CreateOrderResponse(Guid OrderId, string ClientId);

public sealed record GetOrderResponse(Guid OrderId, string ClientId, string Item, int Quantity, string Status);

/// <summary>訂單狀態：active（有效）或 cancelled（已取消）。</summary>
public sealed record StoredOrder(string ClientId, CreateOrderRequest Request, string Status);

public enum CreateOutcome
{
    /// <summary>首次成功提交本地訂單。</summary>
    Created,

    /// <summary>同一業務識別與內容已完成，回傳既有訂單，未重做副作用。</summary>
    Replayed,

    /// <summary>同一業務識別仍處理中（租約有效），未啟動第二次執行。</summary>
    InProgress,

    /// <summary>同一 Idempotency-Key 對應不同業務識別，未覆寫既有操作。</summary>
    KeyConflict,

    /// <summary>同一業務識別的業務內容不同，未覆寫既有訂單。</summary>
    IdentityConflict,

    /// <summary>業務提交無法確認（提交前當機或提交後回應遺失），呼叫端須以同一業務識別重試。</summary>
    CommitUnavailable,
}

public sealed record CreateResult(CreateOutcome Outcome, Guid? OrderId = null);

/// <summary>測試用故障注入：模擬提交前當機（不寫入、處理中租約保留）與提交後回應遺失（已寫入、回應未送達）。</summary>
public enum CommitFault
{
    None,
    CrashBeforeCommit,
    LoseResponseAfterCommit,
}

/// <summary>測試用暫停點：提交前暫停，直到測試放行；用於觀察處理中狀態。</summary>
public sealed class CommitHold
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>已有請求進入暫停點。</summary>
    public Task Entered => _entered.Task;

    public void Release() => _released.TrySetResult();

    internal async Task WaitAsync()
    {
        _entered.TrySetResult();
        await _released.Task;
    }
}

/// <summary>模擬提交前後故障時拋出；控制器轉為 503 business_commit_unavailable。</summary>
internal sealed class SimulatedCommitFailureException : Exception { }

/// <summary>
/// 建立訂單的本地業務狀態（lab：記憶體，所有執行個體共用）。
/// - 業務識別 (client_id, orderReference)：決定業務操作是否相同；完成後永久保存，不因 Idempotency-Key 過期而移除。
/// - Idempotency-Key (client_id, key)：定位請求嘗試並指向業務識別；保存 24 小時（涵蓋 10 分鐘最長重試期）。
/// - 處理中租約 30 秒；到期後同一業務操作可由新嘗試接手，舊嘗試於提交時因 AttemptId 不符而不寫入（fencing）。
/// - 去重狀態與訂單寫入於同一把鎖內同時完成，因此不會出現「已寫訂單但去重狀態未完成」的中間狀態。
/// 保證範圍：本業務服務的本地提交只發生一次；不宣稱下游或跨系統 exactly-once（見 06 單）。
/// </summary>
public sealed class OrderStore
{
    /// <summary>lab 暫定：處理中租約（待使用者確認）。</summary>
    public static readonly TimeSpan ProcessingLease = TimeSpan.FromSeconds(30);

    /// <summary>lab 暫定：Idempotency-Key 紀錄保存期（待使用者確認），涵蓋最長重試期。</summary>
    public static readonly TimeSpan IdempotencyKeyRetention = TimeSpan.FromHours(24);

    private readonly object _gate = new();
    private readonly Dictionary<Guid, StoredOrder> _orders = new();
    private readonly Dictionary<(string ClientId, string OrderReference), OperationRecord> _operations = new();
    private readonly Dictionary<(string ClientId, string Key), KeyRecord> _keys = new();
    private CommitHold? _hold;
    private CommitFault _armedFault = CommitFault.None;

    /// <summary>本執行個體群組已建立的訂單數（測試用）。</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _orders.Count;
            }
        }
    }

    /// <summary>目前保存的 Idempotency-Key 紀錄數（測試用，驗證查詢不寫入去重紀錄）。</summary>
    public int IdempotencyRecordCount
    {
        get
        {
            lock (_gate)
            {
                return _keys.Count;
            }
        }
    }

    /// <summary>只回傳由同一已驗證 Client 建立的訂單（業務資料範圍檢查）。</summary>
    public bool TryGet(Guid orderId, string verifiedClientId, out StoredOrder? order)
    {
        lock (_gate)
        {
            if (_orders.TryGetValue(orderId, out var stored) && stored.ClientId == verifiedClientId)
            {
                order = stored;
                return true;
            }

            order = null;
            return false;
        }
    }

    /// <summary>只能取消由同一已驗證 Client 建立的訂單；他人訂單不存在、不會被修改。</summary>
    public bool TryCancel(Guid orderId, string verifiedClientId, out StoredOrder? order)
    {
        lock (_gate)
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
    }

    /// <summary>以業務識別取得訂單（測試用：持久業務結果的觀察點）。</summary>
    public bool TryGetByOrderReference(string clientId, string orderReference, out StoredOrder? order)
    {
        lock (_gate)
        {
            order = _orders.Values.FirstOrDefault(item => item.ClientId == clientId && item.Request.OrderReference == orderReference);
            return order is not null;
        }
    }

    /// <summary>同一業務識別的訂單數（測試用：應永遠為 0 或 1）。</summary>
    public int CountByOrderReference(string clientId, string orderReference)
    {
        lock (_gate)
        {
            return _orders.Values.Count(item => item.ClientId == clientId && item.Request.OrderReference == orderReference);
        }
    }

    /// <summary>Idempotency-Key 紀錄的保存至時間（測試用）；紀錄不存在時回傳 null。</summary>
    public DateTimeOffset? KeyRetainUntil(string clientId, string idempotencyKey)
    {
        lock (_gate)
        {
            return _keys.TryGetValue((clientId, idempotencyKey), out var record) ? record.RetainUntil : null;
        }
    }

    /// <summary>
    /// 建立訂單（已授權、已簽章驗證）。依業務識別與 Idempotency-Key 判斷：首次提交、回傳既有結果、處理中、或衝突拒絕。
    /// </summary>
    public async Task<CreateResult> CreateOnceAsync(string clientId, string idempotencyKey, CreateOrderRequest request, DateTimeOffset now)
    {
        var attempt = Guid.NewGuid();
        var fingerprint = ContentFingerprint(request);
        var operationId = (clientId, request.OrderReference);
        var keyId = (clientId, idempotencyKey);

        lock (_gate)
        {
            PruneExpiredKeys(now);
            if (_keys.TryGetValue(keyId, out var existingKey) && existingKey.OrderReference != request.OrderReference)
            {
                return new CreateResult(CreateOutcome.KeyConflict);
            }

            if (_operations.TryGetValue(operationId, out var operation))
            {
                if (operation.Fingerprint != fingerprint)
                {
                    return new CreateResult(CreateOutcome.IdentityConflict);
                }

                if (operation.OrderId is { } completed)
                {
                    _keys[keyId] = new KeyRecord(request.OrderReference, now + IdempotencyKeyRetention);
                    return new CreateResult(CreateOutcome.Replayed, completed);
                }

                if (operation.LeaseUntil > now)
                {
                    _keys[keyId] = new KeyRecord(request.OrderReference, now + IdempotencyKeyRetention);
                    return new CreateResult(CreateOutcome.InProgress);
                }
            }

            // 首次提交，或處理中租約已到期而由本次嘗試接手。
            _operations[operationId] = new OperationRecord(fingerprint, attempt, now + ProcessingLease, null);
            _keys[keyId] = new KeyRecord(request.OrderReference, now + IdempotencyKeyRetention);
        }

        var hold = TakeHold();
        if (hold is not null)
        {
            await hold.WaitAsync();
        }

        try
        {
            lock (_gate)
            {
                if (TakeFault(CommitFault.CrashBeforeCommit))
                {
                    // 模擬當機：不寫入，處理中租約保留，直到到期才可接手。
                    throw new SimulatedCommitFailureException();
                }

                if (!_operations.TryGetValue(operationId, out var current) || current.AttemptId != attempt)
                {
                    // 租約已由其他嘗試接手；本嘗試不得提交。
                    return new CreateResult(CreateOutcome.InProgress);
                }

                var orderId = Guid.NewGuid();
                _orders[orderId] = new StoredOrder(clientId, request, "active");
                _operations[operationId] = current with { OrderId = orderId };

                if (TakeFault(CommitFault.LoseResponseAfterCommit))
                {
                    // 模擬提交後回應遺失：訂單與去重狀態已一併寫入，但回應未送達呼叫端。
                    throw new SimulatedCommitFailureException();
                }

                return new CreateResult(CreateOutcome.Created, orderId);
            }
        }
        catch (SimulatedCommitFailureException)
        {
            return new CreateResult(CreateOutcome.CommitUnavailable);
        }
    }

    /// <summary>測試用：下一次業務提交於寫入前暫停。</summary>
    public CommitHold HoldNextCommit()
    {
        lock (_gate)
        {
            _hold = new CommitHold();
            return _hold;
        }
    }

    /// <summary>測試用：下一次業務提交套用指定故障（只生效一次）。</summary>
    public void ArmFault(CommitFault fault)
    {
        lock (_gate)
        {
            _armedFault = fault;
        }
    }

    /// <summary>測試用：將所有處理中租約設為已到期（模擬提交前當機後租約到期）。</summary>
    public void ExpireLeasesForTest()
    {
        lock (_gate)
        {
            var expired = DateTimeOffset.UtcNow.AddSeconds(-1);
            foreach (var key in _operations.Keys.ToList())
            {
                var operation = _operations[key];
                if (operation.OrderId is null)
                {
                    _operations[key] = operation with { LeaseUntil = expired };
                }
            }
        }
    }

    /// <summary>測試用：將所有 Idempotency-Key 紀錄設為已過期（業務識別仍保留）。</summary>
    public void ExpireKeyRecordsForTest()
    {
        lock (_gate)
        {
            var expired = DateTimeOffset.UtcNow.AddSeconds(-1);
            foreach (var key in _keys.Keys.ToList())
            {
                _keys[key] = _keys[key] with { RetainUntil = expired };
            }
        }
    }

    /// <summary>業務內容比對：只取決定業務意義的欄位，排除 nonce、簽章與 Token。</summary>
    private static string ContentFingerprint(CreateOrderRequest request)
        => JsonSerializer.Serialize(new { request.Item, request.Quantity });

    private void PruneExpiredKeys(DateTimeOffset now)
    {
        foreach (var key in _keys.Where(entry => entry.Value.RetainUntil < now).Select(entry => entry.Key).ToList())
        {
            _keys.Remove(key);
        }
    }

    private CommitHold? TakeHold()
    {
        lock (_gate)
        {
            var hold = _hold;
            _hold = null;
            return hold;
        }
    }

    /// <summary>必須在持有 _gate 時呼叫。</summary>
    private bool TakeFault(CommitFault fault)
    {
        if (_armedFault != fault)
        {
            return false;
        }

        _armedFault = CommitFault.None;
        return true;
    }

    private sealed record OperationRecord(string Fingerprint, Guid AttemptId, DateTimeOffset LeaseUntil, Guid? OrderId);

    private sealed record KeyRecord(string OrderReference, DateTimeOffset RetainUntil);
}
