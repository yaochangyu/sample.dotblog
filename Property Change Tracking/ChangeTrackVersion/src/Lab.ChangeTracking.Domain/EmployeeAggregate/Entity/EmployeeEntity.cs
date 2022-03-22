using Lab.ChangeTracking.Infrastructure.DB.EntityModel;

namespace Lab.ChangeTracking.Domain;

public record EmployeeEntity
{
    public virtual Guid Id { get; set; }

    public virtual ChangeState EntityState { get; set; }

    public virtual string Name { get; set; }

    public virtual int? Age { get; set; }

    public virtual string Remark { get; set; }

    public virtual List<AddressEntity> Addresses { get; set; } = new();

    public virtual IdentityEntity Identity { get; set; }

    public virtual string ModifiedBy { get; set; }

    public virtual DateTimeOffset? ModifiedAt { get; set; }

    public virtual DateTimeOffset CreatedAt { get; set; }

    public virtual string CreatedBy { get; set; }

    public virtual int Version { get; set; }
}