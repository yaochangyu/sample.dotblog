using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate;

public interface IEmployeeAggregate
{
    Task<EmployeeEntity> ModifyFlowAsync(EmployeeEntity employee, CancellationToken cancel = default);
}