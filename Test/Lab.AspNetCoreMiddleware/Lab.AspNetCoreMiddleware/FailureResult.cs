namespace Lab.AspNetCoreMiddleware;

internal class FailureResult
{
    public string Code { get; init; }

    public string PropertyName { get; init; }

    public string Messages { get; init; }

    public string Value { get; init ; }
}