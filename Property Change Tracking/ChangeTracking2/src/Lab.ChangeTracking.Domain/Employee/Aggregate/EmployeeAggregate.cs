using ChangeTracking;

namespace Lab.ChangeTracking.Domain;

public class EmployeeAggregate : IEmployeeAggregate
{
    private readonly IEmployeeRepository _repository;

    public EmployeeAggregate(IEmployeeRepository repository)
    {
        this._repository = repository;
    }

    public async Task<EmployeeEntity> ModifyFlowAsync(EmployeeEntity srcEmployee, CancellationToken cancel = default)
    {
        var memberTrackable = srcEmployee.AsTrackable();

        memberTrackable.Remark = "我變了";
        memberTrackable.Identity.Remark = "我變了";
        memberTrackable.Addresses[0].Remark = "我變了";
        memberTrackable.Addresses.RemoveAt(1);
        var changeCount = await this._repository.SaveChangeAsync(memberTrackable, cancel);
        return memberTrackable;
    }
}