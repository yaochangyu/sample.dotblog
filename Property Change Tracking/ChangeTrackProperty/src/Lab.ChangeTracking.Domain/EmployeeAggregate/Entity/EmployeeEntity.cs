using Lab.ChangeTracking.Infrastructure.DB.EntityModel;

namespace Lab.ChangeTracking.Domain;

public record EmployeeEntity : EntityBase
{
    public string Name
    {
        get => this._name;
        init => this._name = value;
    }

    public int? Age
    {
        get => this._age;
        init => this._age = value;
    }

    public string Remark
    {
        get => this._remark;
        init => this._remark = value;
    }

    public List<AddressEntity> Addresses { get; init; }

    public IdentityEntity Identity { get; init; }

    private int? _age;
    private string _name;
    private string _remark;

    /// <summary>
    ///     從資料庫查到之後放進去
    /// </summary>
    /// <param name="employee"></param>
    /// <returns></returns>
    public EmployeeEntity AsTrackable(Employee employee)
    {
        this._changedProperties.Clear();
        this._originalValues.Clear();
        this._entityState = EntityState.Unchanged;
        this._commitState = CommitState.Unchanged;
        this._id = employee.Id;
        this._version = employee.Version;
        this._createdAt = employee.CreatedAt;
        this._createdBy = employee.CreatedBy;
        this._modifiedAt = employee.ModifiedAt;
        this._modifiedBy = employee.ModifiedBy;
        this._version = employee.Version;
        this._name = employee.Name;
        this._age = employee.Age;
        this._remark = employee.Remark;

        // Addresses = null,
        // Identity = null,

        this.AsTrackable();
        return this;
    }

    public EmployeeEntity SetDelete()
    {
        this._entityState = EntityState.Deleted;
        return this;
    }

    public EmployeeEntity New(string name, int age, string remark = null)
    {
        this._entityState = EntityState.Added;
        this._commitState = CommitState.Unchanged;
        this._version = 1;
        this._name = name;
        this._age = age;
        this._remark = remark;
        return this;
    }

    public override void RejectChanges()
    {
        throw new NotImplementedException();
    }

    public EmployeeEntity SetProfile(string name, int age, string remark = null)
    {
        this._name = name;
        this._age = age;
        this._remark = remark;
        this.ChangeTrack(nameof(this.Name), name);
        this.ChangeTrack(nameof(this.Age), age);
        this.ChangeTrack(nameof(this.Remark), remark);
        return this;
    }
}