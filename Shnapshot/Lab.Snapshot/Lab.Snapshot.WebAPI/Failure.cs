using System.Text.Json.Serialization;

namespace Lab.Snapshot.WebAPI;

public enum FailureCode
{
    MemberExist,
    MemberNotExist
}

public class Failure
{
    public FailureCode Code { get; set; }

    public string Message { get; set; }

    public Failure(FailureCode code, string message)
    {
        this.Code = code;
        this.Message = message;
    }

    public List<Failure> Failures { get; set; }

    [JsonIgnore]
    public Exception Fetal { get; set; }
}