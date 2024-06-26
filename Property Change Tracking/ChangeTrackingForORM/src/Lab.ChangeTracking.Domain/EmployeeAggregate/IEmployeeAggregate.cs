﻿using Lab.ChangeTracking.Abstract;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate;

public interface IEmployeeAggregate<T> : IAggregationRoot<T> where T : IChangeTrackable
{
    IEmployeeAggregate<T> SetName(string name);

    IEmployeeAggregate<T> SetAge(int age);

    void SaveChangeAsync(CancellationToken cancel = default);
}