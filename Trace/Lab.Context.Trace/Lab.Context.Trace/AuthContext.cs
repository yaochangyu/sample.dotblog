namespace Lab.Context.Trace;

public record AuthContext
{
    public string TraceId { get; init; }

    public string UserId { get; init; }
}