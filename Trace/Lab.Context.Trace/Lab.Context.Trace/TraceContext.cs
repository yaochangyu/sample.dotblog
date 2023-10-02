namespace Lab.Context.Trace;

public record TraceContext
{
    public string TraceId { get; init; }

    public string UserId { get; init; }
}