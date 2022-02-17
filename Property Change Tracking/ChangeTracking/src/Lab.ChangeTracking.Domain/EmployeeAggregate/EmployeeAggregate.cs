using ChangeTracking;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate;

public class EmployeeAggregate : IEmployeeAggregate
{
    private IEmployeeRepository _repository;

    public EmployeeAggregate(IEmployeeRepository repository)
    {
        this._repository = repository;
    }

    public async Task<EmployeeEntity> ModifyAsync(EmployeeEntity employee, CancellationToken cancel = default)
    {
        var trackable = employee.AsTrackable();
        trackable.Age = 20;
        trackable.Name = "小章";
        var changeCount = await this._repository.ChangeAsync(trackable, cancel);
        return trackable;
    }


}