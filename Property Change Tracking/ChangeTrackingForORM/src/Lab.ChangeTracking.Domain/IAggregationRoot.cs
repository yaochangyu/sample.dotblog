using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Domain;

public interface IAggregationRoot<T> where T : IChangeTrackable
{
    IReadOnlyList<Action<T>> GetChangeActions();

    void SetInstance(T instance);

    T GetInstance();

    /// <summary>
    ///     SubmitChange 後則進版號
    /// </summary>
    /// <param name="when"></param>
    /// <param name="who"></param>
    /// <returns></returns>
    (Error<string> err, bool changed) SubmitChange(DateTimeOffset when, string who);

    void ChangeTrack(Action<T> change);
}