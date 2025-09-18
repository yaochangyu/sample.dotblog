using System.Collections.Concurrent;
using System.Threading.Channels;
using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

public class ChannelRequestQueue : IRequestQueue
{
    private readonly Channel<QueuedRequest> _channel;
    private readonly ChannelWriter<QueuedRequest> _writer;
    private readonly ChannelReader<QueuedRequest> _reader;
    private readonly ConcurrentDictionary<string, QueuedRequest> _pendingRequests = new();

    public ChannelRequestQueue(int capacity = 100)
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

