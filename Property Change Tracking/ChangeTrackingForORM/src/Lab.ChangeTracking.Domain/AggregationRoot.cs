using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Domain;

public abstract class AggregationRoot<T> : IAggregationRoot<T> where T : IChangeTrackable
{
    public EntityState State { get; protected set; }

    /// <summary>
    ///     建立時間
    /// </summary>
    public DateTimeOffset CreatedAt
    {
        get => this._instance.CreatedAt;
        init => this._instance.CreatedAt = value;
    }

    /// <summary>
    ///     建立者
    /// </summary>
    public string CreatedBy
    {
        get => this._instance.CreatedBy;
        init => this._instance.CreatedBy = value;
    }

    /// <summary>
    ///     異動時間
    /// </summary>
    public DateTimeOffset UpdatedAt
    {
        get => this._instance.UpdatedAt;
        init => this._instance.UpdatedAt = value;
    }

    /// <summary>
    ///     異動者
    /// </summary>
    public string UpdatedBy
    {
        get => this._instance.UpdatedBy;
        init => this._instance.UpdatedBy = value;
    }

    /// <summary>
    ///     異動版號
    /// </summary>
    public int Version
    {
        get => this._instance.Version;
        init => this._instance.Version = value;
    }

    private readonly IList<Action<T>> _changeActions = new List<Action<T>>();
    protected readonly Dictionary<string, object> ChangedProperties = new();
    protected readonly Dictionary<string, object> OriginalValues = new();
    protected IUUIdProvider _uuIdProvider;
    protected T _instance;
    protected ISystemClock _systemClock;
    protected IAccessContext _accessContext;

    public IReadOnlyList<Action<T>> GetChangeActions()
    {
        return this._changeActions.ToList();
    }

    public void SetInstance(T instance)
    {
        this._instance = instance;
        this.State = EntityState.Unchanged;
    }

    /// <summary>
    ///     SubmitChange 後則進版號
    /// </summary>
    /// <returns></returns>
    public (Error<string> err, bool changed) SubmitChange()
    {
        var (now,accessUserId )= (this._accessContext.AccessNow,this._accessContext.AccessUserId);
        if (this.State == EntityState.Submitted)
        {
            return (
                new Error<string>("STATE_CONFLICT",
                                  $"Entity({this.State}) was submitted and should not submit again."), false);
        }

        if (this.State == EntityState.Unchanged)
        {
            return (null, false);
        }

        if (this.State == EntityState.Added)
        {
            this.ChangeTrack(x => x.CreatedAt = now);
            this.ChangeTrack(x => x.CreatedBy = accessUserId);
            this.ChangeTrack(x => x.UpdatedAt = now);
            this.ChangeTrack(x => x.UpdatedBy = accessUserId);
            this.ChangeTrack(x => x.Version += 1);
        }
        else
        {
            this.ChangeTrack(x => x.UpdatedAt = now);
            this.ChangeTrack(x => x.UpdatedBy = accessUserId);
            this.ChangeTrack(x => x.Version += 1);
        }

        this.State = EntityState.Submitted;

        return (null, true);
    }

    public void ChangeTrack(Action<T> changeAction)
    {
        if (this.State == EntityState.Submitted)
        {
            throw new Exception("已經 Submitted 的 Doamin，無法再進行修改。");
        }

        changeAction(this._instance);
        this._changeActions.Add(changeAction);

        if (this.State == EntityState.Unchanged)
        {
            this.State = EntityState.Modified;
        }
    }

    private T Clone(T source)
    {
        return (T)Activator.CreateInstance(typeof(T));
    }
}