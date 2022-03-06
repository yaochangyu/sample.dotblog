namespace Lab.ChangeTracking.Domain;

public abstract record EntityBase : IChangeTrackable
{
    public Guid? Id
    {
        get => this._id;
        init => this._id = value;
    }

    protected readonly Dictionary<string, object> _changedProperties = new();
    protected readonly Dictionary<string, object> _originalValues = new();
    protected CommitState _commitState;
    private DateTimeOffset? _createdAt;
    private string? _createdBy;
    protected EntityState _entityState;
    private Guid? _id;
    private DateTimeOffset? _modifiedAt;
    private string? _modifiedBy;
    protected int _version;

    public EntityBase AsTrackable()
    {
        this.Validate();

        // this._entityState = EntityState.Added;
        // this._commitState = CommitState.Unchanged;
        // this._version = 1;
        var properties = this.GetType().GetProperties();
        foreach (var property in properties)
        {
            this._originalValues.Add(property.Name, property.GetValue(this));
        }

        return this;
    }

    public (Error<string> err, bool changed) AcceptChanges(ISystemClock systemClock,
                                                           IAccessContext accessContext,
                                                           IUUIdProvider idProvider)
    {
        this.Validate();

        this._commitState = CommitState.Accepted;
        var (now, accessUserId) = (systemClock.GetNow(), accessContext.GetUserId());

        if (this.EntityState == EntityState.Unchanged)
        {
            return (null, false);
        }

        if (this.EntityState == EntityState.Added)
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

        // this._entityState = EntityState.Submitted;

        return (null, true);
    }

    public abstract void Reset();

    public Dictionary<string, object> GetChangedProperties()
    {
        return this._changedProperties;
    }

    public EntityState EntityState
    {
        get => this._entityState;
        init => this._entityState = value;
    }

    public CommitState CommitState
    {
        get => this._commitState;
        init => this._commitState = value;
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

    public void ChangeTrack(string propertyName, object value)
    {
        this.Validate();

        var changes = this._changedProperties;
        var originals = this._originalValues;
        if (changes.ContainsKey(propertyName) == false)
        {
            if (originals[propertyName] != value)
            {
                changes.Add(propertyName, value);
                this._entityState = EntityState.Added;
            }
        }
        else
        {
            if (originals[propertyName] != changes[propertyName])
            {
                changes[propertyName] = value;
                this._entityState = EntityState.Modified;
            }
        }
    }

    private void Validate()
    {
        if (this.CommitState == CommitState.Accepted)
        {
            throw new Exception("已經同意，無法再進行修改");
        }
    }
}