using Lab.ChangeTracking.Abstract;
using Lab.ChangeTracking.Domain.Entity;

namespace Lab.ChangeTracking.Domain.Repository;

public interface IEmployeeRepository
{
    // Task<int> SaveChangeAsync(IEmployeeAggregate<IEmployeeEntity> employee, CancellationToken cancel = default);
    Task<int> SaveChangeAsync(EmployeeAggregate employee, CancellationToken cancel = default);
    Task<int> SaveChangeAsync(IEmployeeEntity employee, CancellationToken cancel = default);

    Task<IEmployeeEntity> GetAsync(Guid id, CancellationToken cancel);
}