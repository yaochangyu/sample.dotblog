using Lab.Domain.Entity;

namespace Lab.Domain;

public interface IEmployeeAggregate
{
    Employee InsertAsync(Employee employee, CancellationToken cancel = default);
}