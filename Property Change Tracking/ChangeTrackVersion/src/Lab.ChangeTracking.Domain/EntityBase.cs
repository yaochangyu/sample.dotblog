using System.Collections;

namespace Lab.ChangeTracking.Domain;

public abstract record EntityBase : IChangeTrackable
{
    // public Guid Id
    // {
    //     get => this._id;
    //     init => this._id = value;
    // }

    protected readonly Dictionary<string, object> _changedProperties = new();
    protected readonly Dictionary<string, object> _originalValues = new();
    protected CommitState _commitState;
    protected ChangeState _entityState;
    protected Guid _id;

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


        if (this.EntityState == ChangeState.Added)
        {
            this._id = idProvider.GenerateId();
            this.CreatedAt = now;
            this.CreatedBy = accessUserId;
            this.Version = 1;
        }
        else
        {
            this.Version = this.Version++;
        }

        this.ModifiedAt = now;
        this.ModifiedBy = accessUserId;

        // this._entityState = EntityState.Submitted;

        return (null, true);
    }

    public abstract void RejectChanges();

    public Dictionary<string, object> GetChangedProperties()
    {
        return this._changedProperties;
    }

    public ChangeState EntityState
    {
        get => this._entityState;
        init => this._entityState = value;
    }

    public CommitState CommitState
    {
        get => this._commitState;
        init => this._commitState = value;
    }

    public int Version { get; set; }

    public Dictionary<string, object> GetOriginalValues()
    {
        return this._originalValues;
    }

    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public void ChangeTrack(string propertyName, object value)
    {
        this.Validate();

        var changes = this._changedProperties;
        var originals = this._originalValues;
        if (originals.Count <= 0)
        {
            throw new Exception("尚未啟用追蹤");
        }

        if (changes.ContainsKey(propertyName) == false)
        {
            if (originals[propertyName] != value)
            {
                changes.Add(propertyName, value);
                this._entityState = ChangeState.Modified;
            }
        }
        else
        {
            if (originals[propertyName].ToString() == value.ToString())
            {
                changes.Remove(propertyName);
            }
        }

        if (changes.Count <= 0)
        {
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