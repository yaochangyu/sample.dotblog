using Lab.ChangeTracking.Domain.Entity;

namespace Lab.ChangeTracking.Domain.Repository;

public interface IEmployeeRepository
{
    Task<int> SaveChangeAsync(EmployeeEntity employee, CancellationToken cancel = default);
}