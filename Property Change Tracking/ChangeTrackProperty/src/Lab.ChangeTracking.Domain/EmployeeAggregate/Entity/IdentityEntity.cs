namespace Lab.ChangeTracking.Domain;

public record IdentityEntity
{
    public virtual string Account { get; set; }

    public virtual string Password { get; set; }

    public virtual string Remark { get; set; }

    public virtual DateTimeOffset CreateAt { get; set; }

    public virtual string CreateBy { get; set; }
}