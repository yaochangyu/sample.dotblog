using Lab.ChangeTracking.Abstract;
using Lab.ChangeTracking.Domain.Entity;

namespace Lab.ChangeTracking.Domain;

public interface IEmployeeAggregate<T> : IAggregationRoot<T> where T : IChangeTrackable
{
    IEmployeeAggregate<T> SetName(string name);

    IEmployeeAggregate<T> SetAge(int age);

    void SaveChangeAsync(CancellationToken cancel = default);
}