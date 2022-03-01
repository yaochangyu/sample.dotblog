using Lab.ChangeTracking.Abstract;
using Lab.ChangeTracking.Domain.Entity;
using Lab.ChangeTracking.Domain.Repository;

namespace Lab.ChangeTracking.Domain;

// public class EmployeeAggregate : AggregationRoot<IEmployeeEntity>, 
//                                  IEmployeeAggregate<IEmployeeEntity>
public class EmployeeAggregate : AggregationRoot<IEmployeeEntity>
{
    public Guid Id => this._instance.Id;

    public string Name => this._instance.Name;

    public int? Age => this._instance.Age;

    public string Remark => this._instance.Remark;

    private readonly IEmployeeRepository _repository;
    private readonly ISystemClock _systemClock;
    private readonly IUUIdProvider _uuIdProvider;

    private string _name;

    public EmployeeAggregate(IEmployeeRepository repository,
                             IUUIdProvider uuIdProvider,
                             ISystemClock systemClock)
    {
        this._repository = repository;
        this._uuIdProvider = uuIdProvider;
        this._systemClock = systemClock;
    }

    public void CreateInstance(string name, int age, string remark = null)
    {
        var now = this._systemClock.Now;
        var instance = new EmployeeEntity
        {
            Id = this._uuIdProvider.GenerateId(),
            Name = name,
            Age = age,
            Version = 1,
            Remark = remark,
            Identity = null
        };
        this.State = EntityState.Added;
        this._instance = instance;
    }

    public async Task GetAsync(Guid id, CancellationToken cancel = default)
    {
        this._instance = await this._repository.GetAsync(id, cancel);

        this.State = EntityState.Unchanged;
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