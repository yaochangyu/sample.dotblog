namespace Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;

public record ProfileEntity
{
    public virtual string FirstName { get; set; }

    public virtual string LastName { get; set; }
}