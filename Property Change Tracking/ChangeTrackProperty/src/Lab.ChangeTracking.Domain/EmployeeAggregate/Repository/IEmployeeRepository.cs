namespace Lab.ChangeTracking.Domain;

public interface IEmployeeRepository
{
    Task<int> SaveChangeAsync(EmployeeEntity employee, CancellationToken cancel = default);
}