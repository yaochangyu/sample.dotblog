using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate;

public interface IEmployeeAggregate
{
    Employee InsertAsync(Employee employee, CancellationToken cancel = default);
}