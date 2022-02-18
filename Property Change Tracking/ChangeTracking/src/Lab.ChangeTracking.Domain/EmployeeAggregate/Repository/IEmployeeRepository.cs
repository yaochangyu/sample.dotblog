using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;

public interface IEmployeeRepository
{
    Task<int> SaveChangeAsync(EmployeeEntity employee, CancellationToken cancel = default);
}