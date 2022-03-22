namespace Lab.ChangeTracking.Domain;

public interface IEmployeeRepository
{
    Task<int> SaveChangeAsync(EmployeeEntity employee,
                              IEnumerable<string> excludeProperties = null,
                              CancellationToken cancel = default);
}