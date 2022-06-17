namespace Lab.AspNetCoreMiddleware;

internal class Failure
{
    public string Code { get; init; }

    public IEnumerable<FailureResult> Messages { get; init; }
}