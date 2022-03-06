namespace Lab.ChangeTracking.Domain;

public interface IChangeState
{
    EntityState EntityState { get; init; }

    CommitState CommitState { get; init; }

    int Version { get; init; }
}