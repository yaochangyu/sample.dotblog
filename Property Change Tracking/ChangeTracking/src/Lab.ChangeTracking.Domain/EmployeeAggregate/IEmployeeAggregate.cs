using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate;

public interface IEmployeeAggregate
{
    Task<EmployeeEntity> ModifyAsync(EmployeeEntity employee, CancellationToken cancel = default);
}