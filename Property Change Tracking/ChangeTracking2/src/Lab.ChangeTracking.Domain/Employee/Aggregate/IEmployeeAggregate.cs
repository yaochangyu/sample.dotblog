namespace Lab.ChangeTracking.Domain;

public interface IEmployeeAggregate
{
    Task<EmployeeEntity> ModifyFlowAsync(EmployeeEntity employee, CancellationToken cancel = default);
}