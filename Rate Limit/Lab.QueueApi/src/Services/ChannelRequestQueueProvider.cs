using System.Collections.Concurrent;
using System.Threading.Channels;
using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

/// <summary>
/// 使用 .NET Channels 實作的請求佇列提供者。
/// </summary>
public class ChannelRequestQueueProvider : IRequestQueueProvider
{
    /// <summary>
    /// 用於儲存排隊請求的 Channel。
    /// </summary>
    private readonly Channel<QueuedRequest> _channel;

    /// <summary>
    /// Channel 的寫入器。
    /// </summary>
    private readonly ChannelWriter<QueuedRequest> _writer;

    /// <summary>
    /// Channel 的讀取器。
    /// </summary>
    private readonly ChannelReader<QueuedRequest> _reader;

    /// <summary>
    /// 儲存待處理請求的並行字典。
    /// </summary>
    private readonly ConcurrentDictionary<string, QueuedRequest> _pendingRequests = new();

    /// <summary>
    /// 初始化 ChannelRequestQueueProvider 的新執行個體。
    /// </summary>
    /// <param name="capacity">佇列的最大容量。</param>
    public ChannelRequestQueueProvider(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<QueuedRequest>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
    }

    /// <summary>
    /// 將請求非同步地加入佇列。
    /// </summary>
    /// <param name="requestData">請求的資料。</param>
    /// <returns>表示非同步操作的 Task，其結果為請求的唯一識別碼。</returns>
    public async Task<string> EnqueueRequestAsync(string requestData)
    {
        var queuedRequest = new QueuedRequest
        {
            RequestData = requestData
        };

        _pendingRequests[queuedRequest.Id] = queuedRequest;
        await _writer.WriteAsync(queuedRequest);
        
        return queuedRequest.Id;
    }

    /// <summary>
    /// 從佇列中非同步地取出請求。
    /// </summary>
    /// <param name="cancellationToken">用於取消操作的 CancellationToken。</param>
    /// <returns>表示非同步操作的 Task，其結果為從佇列中取出的 QueuedRequest，如果佇列已空或已完成，則為 null。</returns>
    public async Task<QueuedRequest?> DequeueRequestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _reader.ReadAsync(cancellationToken);
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
    public async Task<ApiResponse> WaitForResponseAsync(string requestId, TimeSpan timeout)
    {
        if (!_pendingRequests.TryGetValue(requestId, out var queuedRequest))
        {
            return new ApiResponse
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
            return new ApiResponse
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
    public void CompleteRequest(string requestId, ApiResponse response)
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
}