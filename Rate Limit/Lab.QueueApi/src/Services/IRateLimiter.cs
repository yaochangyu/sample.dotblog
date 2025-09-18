namespace Lab.QueueApi.Services;

public interface IRateLimiter
{
    bool IsRequestAllowed();
    void RecordRequest();
    TimeSpan GetRetryAfter();
}

