namespace Lab.Snapshot.WebAPI.ServiceModels;

public class MemberResponse
{
    public string Id { get; set; }

    public Profile Profile { get; set; }

    public List<Account> Accounts { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public int Version { get; set; }
}