using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Domain;

public record EmployeeEntity : IEntity
{
    public virtual Guid Id { get; set; }

    public virtual string Name { get; set; }

    public virtual int? Age { get; set; }

    public virtual int Version { get; set; }

    public virtual IdentityEntity Identity { get; set; }

    public virtual IList<AddressEntity> Addresses { get; set; }

    public virtual DateTimeOffset CreatedAt { get; set; }

    public virtual string CreatedBy { get; set; }

    public virtual DateTimeOffset? ModifiedAt { get; set; }

    public virtual string ModifiedBy { get; set; }

    public virtual string Remark { get; set; }
}