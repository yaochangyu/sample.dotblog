using ChangeTracking;

namespace Lab.ChangeTracking.Domain;

public class EmployeeAggregate
{
    public CommitState CommitState { get; private set; }

    private readonly IAccessContext _accessContext;
    private readonly IUUIdProvider _idProvider;

    private readonly EmployeeRepository _repository;
    private readonly ISystemClock _systemClock;

    private EmployeeEntity _instance;

    public EmployeeAggregate(IUUIdProvider idProvider,
                             ISystemClock systemClock,
                             IAccessContext accessContext,
                             EmployeeRepository repository)
    {
        this._idProvider = idProvider;
        this._systemClock = systemClock;
        this._accessContext = accessContext;
        this._repository = repository;
    }

    public EmployeeAggregate AcceptChanges()
    {
        if (this.CommitState == CommitState.Accepted)
        {
            throw new Exception("已接受核准");
        }

        this.CommitState = CommitState.Accepted;
        var trackable = this._instance.CastToIChangeTrackable();
        if (trackable.IsChanged)
        {
            this._instance.Version++;
        }
        else
        {
            this._instance.Version = 1;
        }

        return this;
    }

    public EmployeeAggregate AddAddress(AddressEntity instance)
    {
        this.ValidateAcceptedState();
        var (when, who) = (this._systemClock.GetNow(), this._accessContext.GetUserId());
        instance.Id = this._idProvider.GenerateId();
        instance.CreatedAt = when;
        instance.CreatedBy = who;

        this._instance.Addresses.Add(instance);
        return this;
    }

    public async Task<int> CommitChangeAsync(CancellationToken cancel = default)
    {
        if (this.CommitState != CommitState.Accepted)
        {
            throw new Exception("未被認可");
        }

        var changeCount = 0;
        switch (this._instance.EntityState)
        {
            case ChangeState.Added:
                changeCount = await this._repository.InsertEmployeeAsync(this._instance, cancel);

                break;
            case ChangeState.Modified:
                changeCount = await this._repository.SaveChangesAsync(this._instance, cancel);

                break;
            case ChangeState.Deleted:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        this.CommitState = CommitState.Unchanged;
        return changeCount;
    }

    public Guid GetEmployeeId()
    {
        return this._instance.Id;
    }

    public EmployeeAggregate NewAddress(Guid employeeId, AddressEntity source)
    {
        this.CommitState = CommitState.Unchanged;

        var target = this._instance
            .Addresses
            .Where(p => p.Employee_Id == source.Employee_Id)
            .FirstOrDefault();

        if (target == null)
        {
            return this;
        }

        var (when, who) = (this._systemClock.GetNow(), this._accessContext.GetUserId());

        target.Id = source.Id;
        target.Country = source.Country;
        target.Street = source.Street;
        target.Remark = source.Remark;
        target.EntityState = ChangeState.Added;
        target.ModifiedBy = who;
        target.ModifiedAt = when;
        return this;
    }

    public Guid NewEmployee(string name,
                            int age,
                            string remark = null)
    {
        this.CommitState = CommitState.Unchanged;

        var (when, who) = (this._systemClock.GetNow(), this._accessContext.GetUserId());

        this._instance = new EmployeeEntity
        {
            Id = this._idProvider.GenerateId(),
            EntityState = ChangeState.Added,
            Name = name,
            Age = age,
            Remark = remark,
            CreatedAt = when,
            CreatedBy = who,
            ModifiedAt = when,
            ModifiedBy = who,
        }.AsTrackable();

        return this._instance.Id;
    }

    public EmployeeAggregate SetEntity(EmployeeEntity instance)
    {
        this.CommitState = CommitState.Unchanged;
        
        this._instance = instance.AsTrackable();
        this._instance.EntityState = ChangeState.Modified;

        return this;
    }

    public void SetProfile(string name,
                           int age,
                           string remark = null)
    {
        this.ValidateAcceptedState();
        this.ValidateAddState();
        var (when, who) =
            (this._systemClock.GetNow(), this._accessContext.GetUserId());
        this._instance.ModifiedBy = who;
        this._instance.ModifiedAt = when;

        this._instance.Age = age;
        this._instance.Name = name;
        this._instance.Remark = remark;
    }

    private void ValidateAcceptedState()
    {
        if (this.CommitState == CommitState.Accepted)
        {
            throw new Exception("已接受核准，不能改變狀態");
        }

        this.ValidateAddState();
    }

    private void ValidateAddState()
    {
        if (this._instance.EntityState == ChangeState.Added)
        {
            throw new Exception("Add 狀態不能異動");
        }
    }
}