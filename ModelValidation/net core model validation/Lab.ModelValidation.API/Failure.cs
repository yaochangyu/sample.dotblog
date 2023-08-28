namespace Lab.ModelValidation.API;

public class Failure
{
    public Failure()
    {
    }

    public Failure(FailureCode code, string message)
    {
        this.Code = code;
        this.Message = message;
    }

    public FailureCode Code { get; init; }

    public string Message { get; init; }

    public object Data { get; init; }

    public string TraceId { get; set; }

    public List<Failure> Details { get; init; }
}