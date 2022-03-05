namespace Lab.ChangeTracking.Domain;

public record EntityBase : IChangeTrackable
{
    public Guid? Id
    {
        get => this._id;
        init => this._id = value;
    }

    private readonly Dictionary<string, object> _changedProperties = new();
    private readonly Dictionary<string, object> _originalValues = new();
    private DateTimeOffset? _createdAt;
    private string? _createdBy;
    private Guid? _id;
    private DateTimeOffset? _modifiedAt;
    private string? _modifiedBy;
    private EntityState _state;
    private int _version;

    public (Error<string> err, bool changed) AcceptChanges(ISystemClock systemClock,
                                                           IAccessContext accessContext,
                                                           IUUIdProvider idProvider)
    {
        if (this.State == EntityState.Submitted)
        {
            return (
                new Error<string>("STATE_CONFLICT",
                                  $"Entity({this.State}) was submitted and should not submit again."), false);
        }

        var (now, accessUserId) = (systemClock.GetNow(), accessContext.GetUserId());

        if (this.State == EntityState.Unchanged)
        {
            return (null, false);
        }

        if (this.State == EntityState.Added)
        {
            this._id = idProvider.GenerateId();
            this._createdAt = now;
            this._createdBy = accessUserId;
            this._version = 1;
        }
        else
        {
            this._version = this._version++;
        }

        this._modifiedAt = now;
        this._modifiedBy = accessUserId;

        this._state = EntityState.Submitted;

        return (null, true);
    }

    public void InitialTrack()
    {
        this._state = EntityState.Added;
        this._version = 1;
        var properties = this.GetType().GetProperties();
        foreach (var property in properties)
        {
            this._originalValues.Add(property.Name, property.GetValue(this));
        }
    }

    public Dictionary<string, object> GetChangedProperties()
    {
        return this._changedProperties;
    }

    public bool HasChanged { get; private set; }

    public EntityState State
    {
        get => this._state;
        init => this._state = value;
    }

    public int Version
    {
        get => this._version;
        init => this._version = value;
    }

    public Dictionary<string, object> GetOriginalValues()
    {
        return this._originalValues;
    }

    protected void ChangeTrack(string propertyName, object value)
    {
        if (this.State == EntityState.Submitted)
        {
            throw new Exception("已經 Submitted ，無法再進行修改。");
        }

        var changes = this._changedProperties;
        var originals = this._originalValues;
        if (changes.ContainsKey(propertyName) == false)
        {
            if (originals[propertyName] != value)
            {
                changes.Add(propertyName, value);
                this._state = EntityState.Added;
            }
        }
        else
        {
            if (originals[propertyName] != changes[propertyName])
            {
                changes[propertyName] = value;
                this._state = EntityState.Modified;
            }
        }
    }

    public DateTimeOffset? CreatedAt
    {
        get => this._createdAt;
        init => this._createdAt = value;
    }

    public string? CreatedBy
    {
        get => this._createdBy;
        init => this._createdBy = value;
    }

    public DateTimeOffset? ModifiedAt
    {
        get => this._modifiedAt;
        init => this._modifiedAt = value;
    }

    public string? ModifiedBy
    {
        get => this._modifiedBy;
        init => this._modifiedBy = value;
    }
}