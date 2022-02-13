using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;

public interface IEmployeeRepository
{
    Task<int> InsertAsync(Employee employee, CancellationToken cancel = default);
}

class EmployeeRepository : IEmployeeRepository
{
    public Task<int> InsertAsync(Employee employee, CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }
}
