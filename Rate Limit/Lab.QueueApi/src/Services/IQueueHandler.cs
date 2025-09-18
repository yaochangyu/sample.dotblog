using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

public interface IQueueHandler
{
    Task<string> EnqueueRequestAsync(string requestData,CancellationToken cancellationToken = default);
    Task<QueuedRequest?> DequeueRequestAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse> WaitForResponseAsync(string requestId, TimeSpan timeout,CancellationToken cancellationToken = default);
    void CompleteRequest(string requestId, ApiResponse response);
    int GetQueueLength();
    Task<bool> TryProcessNextRequestAsync(CancellationToken cancellationToken = default);
}

