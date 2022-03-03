namespace Lab.ChangeTracking.Domain;

public interface IAccessContext
{
    string AccessUserId { get; set; }
     
    DateTimeOffset AccessNow { get; set; }
}