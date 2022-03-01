using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Domain.Entity;

public record EmployeeEntity : IEmployeeEntity
{
    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string UpdatedBy { get; set; }

    public int Version { get; set; }

    public Guid Id { get; set; }

    public string Name { get; set; }

    public int? Age { get; set; }

    public string Remark { get; set; }

    // public IList<ProfileEntity> Profiles { get; set; }

    public IdentityEntity Identity { get; set; }
}