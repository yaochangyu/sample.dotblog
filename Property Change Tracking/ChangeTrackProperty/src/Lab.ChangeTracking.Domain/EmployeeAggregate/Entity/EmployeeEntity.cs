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

    private IAccessContext _accessContext;
    private int? _age;

    // public IList<ProfileEntity> Profiles { get; private set; } = new();
    //
    // public IdentityEntity Identity { get; private set; } = new();

    private IUUIdProvider _idProvider;
    private string _name;
    private string _remark;
    private ISystemClock _systemClock;

    public EmployeeEntity SetId(string name, int age, string remark = null)
    {
        this._name = name;
        this._age = age;
        this._remark = remark;
        this.ChangeTrack(nameof(this.Name), name);
        this.ChangeTrack(nameof(this.Age), age);
        this.ChangeTrack(nameof(this.Remark), remark);
        return this;
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