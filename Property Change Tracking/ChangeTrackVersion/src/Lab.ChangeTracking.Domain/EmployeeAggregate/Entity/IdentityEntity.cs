namespace Lab.ChangeTracking.Domain;

public record IdentityEntity
{
    public Guid Employee_Id { get; set; }

    public string Account { get; set; }

    public string Password { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public virtual string Remark { get; set; }
}