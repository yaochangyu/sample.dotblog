using Lab.Domain.Entity;

namespace Lab.Domain.Repository;

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
