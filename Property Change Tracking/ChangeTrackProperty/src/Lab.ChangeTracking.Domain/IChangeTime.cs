namespace Lab.ChangeTracking.Domain;

public interface IChangeTime
{
    DateTimeOffset? CreatedAt { get; init; }

    string? CreatedBy { get; init; }

    DateTimeOffset? ModifiedAt { get; init; }

    string? ModifiedBy { get; init; }
}

public interface IChangeState
{
    EntityState State { get; init; }

    int Version { get; init; }
}

public interface IChangeTrackable : IChangeTime, IChangeState
{
    bool HasChanged { get; }

    Dictionary<string, object> GetChangedProperties();

    Dictionary<string, object> GetOriginalValues();

    // void SetTrackable();
    //
    // void ChangeTrack(string propertyName, object value);
}