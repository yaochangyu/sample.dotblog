namespace Lab.Snapshot.WebAPI.ServiceModels;

public class InsertMemberRequest
{
    public Account Account { get; set; }

    public Profile Profile { get; set; }
}

public class UpdateMemberRequest
{
    public List<Account> Accounts { get; set; }

    public Profile Profile { get; set; }
}