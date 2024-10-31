using Lab.Sharding.Infrastructure;

namespace Lab.Sharding.WebAPI;

public record TraceContext
{
    public string TraceId { get; init; }

    public string UserId { get; init; }

    public Failure SetTraceId(Failure failure)
    {
        failure.TraceId = this.TraceId;
        return failure;
    }
}