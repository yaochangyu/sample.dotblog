namespace Lab.ErrorHandler.API.Models;

public class GenericResult<T>
{
    public Failure Failure { get; set; }

    public T Data { get; set; }
}