namespace Lab.ChangeTracking.Domain;

public interface IChangeTrackable : IChangeContent, IChangeTime, IChangeState
{
    // bool HasChanged { get; }
    public (Error<string> err, bool changed) AcceptChanges(ISystemClock systemClock,
                                                           IAccessContext accessContext,
                                                           IUUIdProvider idProvider);

    void ChangeTrack(string propertyName, object value);

    void RejectChanges();
}