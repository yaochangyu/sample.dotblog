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
        var memberTrackable = srcEmployee.AsTrackable();
        
        memberTrackable.Name = "小章";
        memberTrackable.Identity.Password = "9527";

        var changeCount = await this._repository.SaveChangeAsync(memberTrackable, cancel);
        return memberTrackable;
    }
}