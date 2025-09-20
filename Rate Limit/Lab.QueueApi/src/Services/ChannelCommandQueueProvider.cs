using System.Collections.Concurrent;
using System.Threading.Channels;
using Lab.QueueApi.Commands;

namespace Lab.QueueApi.Services;

/// <summary>
/// 使用 .NET Channels 實作的請求佇列提供者。
/// </summary>
public class ChannelCommandQueueProvider : ICommandQueueProvider
{
    /// <summary>
    /// 用於儲存排隊請求的 Channel。
    /// </summary>
    private readonly Channel<QueuedContext> _channel;

    /// <summary>
    /// Channel 的寫入器。
    /// </summary>
    private readonly ChannelWriter<QueuedContext> _writer;

    /// <summary>
    /// Channel 的讀取器。
    /// </summary>
    private readonly ChannelReader<QueuedContext> _reader;

    /// <summary>
    /// 儲存待處理請求的並行字典。
    /// </summary>
    private readonly ConcurrentDictionary<string, QueuedContext> _pendingRequests = new();

    /// <summary>
    /// 佇列的最大容量。
    /// </summary>
    private readonly int _maxCapacity;

    /// <summary>
    /// 初始化 ChannelRequestQueueProvider 的新執行個體。
    /// </summary>
    /// <param name="capacity">佇列的最大容量。</param>
    public ChannelCommandQueueProvider(int capacity = 100)
    {
        _maxCapacity = capacity;
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<QueuedContext>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
    }

    /// <summary>
    /// 將請求非同步地加入佇列。
    /// </summary>
    /// <param name="requestData">請求的資料。</param>
    /// <param name="cancel"></param>
    /// <returns>表示非同步操作的 Task，其結果為請求的唯一識別碼。</returns>
    public async Task<string> EnqueueCommandAsync(object requestData, CancellationToken cancel = default)
    {
        var queuedRequest = new QueuedContext
        {
            RequestData = requestData
        };

        _pendingRequests[queuedRequest.Id] = queuedRequest;
        await _writer.WriteAsync(queuedRequest, cancel);

        return queuedRequest.Id;
    }

    /// <summary>
    /// 從佇列中非同步地取出請求。
    /// </summary>
    /// <param name="cancellationToken">用於取消操作的 CancellationToken。</param>
    /// <param name="cancel"></param>
    /// <returns>表示非同步操作的 Task，其結果為從佇列中取出的 QueuedRequest，如果佇列已空或已完成，則為 null。</returns>
    public async Task<QueuedContext?> DequeueCommandAsync(CancellationToken cancel = default)
    {
        try
        {
            return await _reader.ReadAsync(cancel);
        }
        catch (InvalidOperationException)
        {
            // Channel was completed
            return null;
        }
    }

    /// <summary>
    /// 非同步地等待特定請求的回應。
    /// </summary>
    /// <param name="requestId">要等待的請求的唯一識別碼。</param>
    /// <param name="timeout">等待的逾時時間。</param>
    /// <returns>表示非同步操作的 Task，其結果為 ApiResponse。</returns>
    public async Task<QueuedCommandResponse> WaitForResponseAsync(string requestId, TimeSpan timeout)
    {
        if (!_pendingRequests.TryGetValue(requestId, out var queuedRequest))
        {
            return new QueuedCommandResponse
            {
                Success = false,
                Message = "Request not found"
            };
        }

        try
        {
            using var cts = new CancellationTokenSource(timeout);
            return await queuedRequest.CompletionSource.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return new QueuedCommandResponse
            {
                Success = false,
                Message = "Request timeout"
            };
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    /// <summary>
    /// 標示一個請求已完成，並設定其回應。
    /// </summary>
    /// <param name="requestId">已完成的請求的唯一識別碼。</param>
    /// <param name="response">請求的 ApiResponse。</param>
    public void CompleteCommand(string requestId, QueuedCommandResponse response)
    {
        if (_pendingRequests.TryGetValue(requestId, out var queuedRequest))
        {
            queuedRequest.CompletionSource.SetResult(response);
        }
    }

    /// <summary>
    /// 取得目前佇列的長度。
    /// </summary>
    /// <returns>佇列中的請求數量。</returns>
    public int GetQueueLength()
    {
        return _pendingRequests.Count;
    }

    /// <summary>
    /// 取得佇列中所有待處理的請求。
    /// </summary>
    /// <param name="cancel"></param>
    /// <returns>包含所有待處理請求的集合。</returns>
    public async Task<IEnumerable<QueuedContext>> GetAllQueuedCommands(CancellationToken cancel = default)
    {
        return _pendingRequests.Values.OrderBy(x => x.QueuedAt);
    }

    /// <summary>
    /// 檢查佇列是否已滿。
    /// </summary>
    /// <returns>如果佇列已滿則返回 true，否則返回 false。</returns>
    public bool IsQueueFull()
    {
        return _pendingRequests.Count >= _maxCapacity;
    }

    /// <summary>
    /// 取得佇列的最大容量。
    /// </summary>
    /// <returns>佇列的最大容量。</returns>
    public int GetMaxCapacity()
    {
        return _maxCapacity;
    }
}