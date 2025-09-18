using System.Collections.Concurrent;
using System.Threading.Channels;
using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

public class ChannelQueueHandler : IQueueHandler
{
    private readonly Channel<QueuedRequest> _channel;
    private readonly ChannelWriter<QueuedRequest> _writer;
    private readonly ChannelReader<QueuedRequest> _reader;
    private readonly ConcurrentDictionary<string, QueuedRequest> _pendingRequests = new();

    public ChannelQueueHandler(int capacity = 100)
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

    public async Task<string> EnqueueRequestAsync(string requestData, CancellationToken cancellationToken = default)
    {
        var queuedRequest = new QueuedRequest
        {
            RequestData = requestData
        };

        _pendingRequests[queuedRequest.Id] = queuedRequest;
        await _writer.WriteAsync(queuedRequest, cancellationToken);

        return queuedRequest.Id;
    }

    public async Task<QueuedRequest?> DequeueRequestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 先檢查是否有可用項目，避免無限等待
            if (_reader.TryRead(out var item))
            {
                return item;
            }

            // 如果沒有立即可用的項目，等待一小段時間
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            return await _reader.ReadAsync(combined.Token);
        }
        catch (OperationCanceledException)
        {
            // 超時或取消
            return null;
        }
        catch (InvalidOperationException)
        {
            // Channel was completed
            return null;
        }
    }
    public async Task<ApiResponse> WaitForResponseAsync(string requestId, TimeSpan timeout, CancellationToken cancellationToken = default)
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

    public void CompleteRequest(string requestId, ApiResponse response)
    {
        if (_pendingRequests.TryGetValue(requestId, out var queuedRequest))
        {
            queuedRequest.CompletionSource.SetResult(response);
        }
    }

    public int GetQueueLength()
    {
        return _pendingRequests.Count;
    }
}
