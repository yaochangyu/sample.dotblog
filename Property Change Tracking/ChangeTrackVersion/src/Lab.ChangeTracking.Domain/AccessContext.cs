namespace Lab.ChangeTracking.Domain;

public interface IAccessContext
{
    public string? GetUserId();
}

public class AccessContext : IAccessContext
{
    public string? GetUserId()
    {
        return "Sys";
    }
}