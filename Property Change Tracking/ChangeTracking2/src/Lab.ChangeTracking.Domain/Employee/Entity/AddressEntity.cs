using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Domain;

public record AddressEntity:IEntity
{
    public virtual Guid Id { get; set; }

    public virtual Guid Employee_Id { get; set; }

    public virtual string Country { get; set; }

    public virtual string Street { get; set; }

    public virtual DateTimeOffset CreatedAt { get; set; }

    public virtual string CreatedBy { get; set; }

    public virtual DateTimeOffset? ModifiedAt { get; set; }

    public virtual string ModifiedBy { get; set; }

    public virtual string Remark { get; set; }
}