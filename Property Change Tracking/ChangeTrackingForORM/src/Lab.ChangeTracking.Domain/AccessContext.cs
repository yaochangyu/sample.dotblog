namespace Lab.ChangeTracking.Domain;

public class AccessContext : IAccessContext
{
    public string AccessUserId { get; set; }

    public DateTimeOffset AccessNow { get; set; }
}