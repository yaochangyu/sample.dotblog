namespace Lab.ErrorHandler.API;

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

    public Exception Exception { get; set; }

    public List<Failure> Details { get; init; } = new();

    public Failure WithoutException()
    {
        List<Failure> details = new();
        foreach (var detail in this.Details)
        {
            details.Add(this.WithoutException(detail));
        }

        return new Failure(this.Code, this.Message)
        {
            Data = this.Data,
            Details = details,
            TraceId = this.TraceId,
        };
    }

    public Failure WithoutException(Failure error)
    {
        var result = new Failure(error.Code, error.Message)
        {
            TraceId = error.TraceId
        };

        foreach (var detailError in this.Details)
        {
            // 遞迴處理 Details 屬性
            var detailResult = this.WithoutException(detailError);
            result.Details.Add(detailResult);
        }

        return result;
    }
}