namespace Lab.Snapshot.DB;

public record MemberDataEntity
{
    public string Id { get; set; }

    public Profile Profile { get; set; }

    public List<Account> Accounts { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public int Version { get; set; }

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { nameof(this.Id), this.Id },
            { nameof(this.Profile), this.Profile },
            { nameof(this.Accounts), this.Accounts },
            { nameof(this.CreatedAt), this.CreatedAt },
            { nameof(this.CreatedBy), this.CreatedBy },
            { nameof(this.UpdatedAt), this.UpdatedAt },
            { nameof(this.UpdatedBy), this.UpdatedBy },
            { nameof(this.Version), this.Version }
        };
    }
}