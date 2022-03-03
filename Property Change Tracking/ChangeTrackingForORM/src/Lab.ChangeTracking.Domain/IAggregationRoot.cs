using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Domain;

public interface IAggregationRoot<T> where T : IChangeTrackable
{
    IReadOnlyList<Action<T>> GetChangeActions();

    void SetInstance(T instance);

    /// <summary>
    ///     SubmitChange 後則進版號
    /// </summary>
    /// <returns></returns>
    (Error<string> err, bool changed) SubmitChange();

    void ChangeTrack(Action<T> change);
}