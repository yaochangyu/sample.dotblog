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

    public async Task<EmployeeEntity> ModifyFlowAsync(EmployeeEntity srcEmployee, CancellationToken cancel = default)
    {
        var trackable = srcEmployee.AsTrackable();

        trackable.Name = "小章";
        trackable.Identity.Password = "9527";
        // trackable.Profiles.Add("FirstName", "余");
        trackable.Profiles = new Dictionary<string, string>() { { "Last", "小章" } };

        var changeCount = await this._repository.SaveChangeAsync(trackable, cancel);
        return trackable;
    }
}