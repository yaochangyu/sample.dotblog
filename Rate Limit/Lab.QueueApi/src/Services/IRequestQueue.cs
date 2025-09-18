using Lab.QueueApi.Models;

namespace Lab.QueueApi.Services;

public interface IRequestQueue
{
    Task<string> EnqueueRequestAsync(string requestData);
    Task<QueuedRequest?> DequeueRequestAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse> WaitForResponseAsync(string requestId, TimeSpan timeout);
    void CompleteRequest(string requestId, ApiResponse response);
    int GetQueueLength();
}

