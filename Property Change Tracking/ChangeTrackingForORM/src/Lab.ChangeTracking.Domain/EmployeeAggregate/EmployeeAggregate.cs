using Lab.ChangeTracking.Abstract;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate;

// public class EmployeeAggregate : AggregationRoot<IEmployeeEntity>, 
//                                  IEmployeeAggregate<IEmployeeEntity>
public class EmployeeAggregate : AggregationRoot<IEmployeeEntity>
{
    public Guid Id => this._instance.Id;

    public string Name => this._instance.Name;

    public int? Age => this._instance.Age;

    public string Remark => this._instance.Remark;

    private readonly IEmployeeRepository _repository;

    public EmployeeAggregate(IEmployeeRepository repository,
                             IUUIdProvider uuIdProvider,
                             ISystemClock systemClock,
                             IAccessContext accessContext)
    {
        this._repository = repository;
        this._uuIdProvider = uuIdProvider;
        this._accessContext = accessContext;
        this._systemClock = systemClock;
    }

    public async Task GetAsync(Guid id, CancellationToken cancel = default)
    {
        this._instance = await this._repository.GetAsync(id, cancel);

        this.State = EntityState.Unchanged;
    }

    public void Initial(string name, int age, string remark = null)
    {
        this._instance = new EmployeeEntity();

        this.ChangeTrack(p => p.Id = this._uuIdProvider.GenerateId());
        this.ChangeTrack(p => p.Age = age);
        this.ChangeTrack(p => p.Name = name);
        this.ChangeTrack(p => p.Version = 0);
        this.ChangeTrack(p => p.Remark = remark);
        this.State = EntityState.Added;
    }

    public async Task<int> SaveChangeAsync(CancellationToken cancel = default)
    {
        return await this._repository.SaveChangeAsync(this, cancel);
    }

    public EmployeeAggregate SetAge(int age)
    {
        var instance = this._instance;
        if (instance.Age != age)
        {
            this.ChangeTrack(p => p.Age = age);
        }

        return this;
    }

    // public IEmployeeAggregate<IEmployeeEntity> SetName(string name)
    public EmployeeAggregate SetName(string name)
    {
        var instance = this._instance;
        if (instance.Name != name)
        {
            this.ChangeTrack(p => p.Name = name);
        }

        return this;
    }
}