using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Domain;

public abstract class AggregationRoot<T> where T : IChangeTrackable
{
    public EntityState State { get; private set; }

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

    private readonly IList<Action<T>> _changes = new List<Action<T>>();
    protected readonly Dictionary<string, object> ChangedProperties = new();
    protected readonly Dictionary<string, object> OriginalValues = new();

    protected T _instance;

    public AggregationRoot(T instance)
    {
        this._instance = instance;
    }

    public IReadOnlyList<Action<T>> GetInstanceChanges()
    {
        return this._changes.ToList();
    }

    public T GetInstance()
    {
        return this._instance;
    }

    /// <summary>
    ///     SubmitChange 後則進版號
    /// </summary>
    /// <param name="when"></param>
    /// <param name="who"></param>
    /// <returns></returns>
    public (Error<string> err, bool changed) SubmitChange(DateTimeOffset when, string who)
    {
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
            this.Track(x => x.CreatedAt = when);
            this.Track(x => x.CreatedBy = who);
            this.Track(x => x.UpdatedAt = when);
            this.Track(x => x.UpdatedBy = who);
            this.Track(x => x.Version += 1);
        }
        else
        {
            this.Track(x => x.UpdatedAt = when);
            this.Track(x => x.UpdatedBy = who);
            this.Track(x => x.Version += 1);
        }

        this.State = EntityState.Submitted;

        return (null, true);
    }

    protected void Track(Action<T> change)
    {
        if (this.State == EntityState.Submitted)
        {
            throw new Exception("已經 Submitted 的 Doamin，無法再進行修改。");
        }

        change(this._instance);
        this._changes.Add(change);

        if (this.State == EntityState.Unchanged)
        {
            this.State = EntityState.Modified;
        }
    }
}