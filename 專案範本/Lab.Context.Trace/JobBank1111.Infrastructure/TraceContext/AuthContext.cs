namespace JobBank1111.Infrastructure.TraceContext;

public record AuthContext
{
    public string TraceId { get; init; }

    public string UserId { get; init; }
}