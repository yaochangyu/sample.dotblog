
namespace Lab.ChangeTracking.Domain;

public record IdentityEntity 
{
    public virtual Guid Employee_Id { get; set; }

    public virtual string Account { get; set; }

    public virtual string Password { get; set; }

    public virtual DateTimeOffset CreatedAt { get; set; }

    public virtual string CreatedBy { get; set; }

    public virtual DateTimeOffset? ModifiedAt { get; set; }

    public virtual string? ModifiedBy { get; set; }

    public virtual string Remark { get; set; }

}