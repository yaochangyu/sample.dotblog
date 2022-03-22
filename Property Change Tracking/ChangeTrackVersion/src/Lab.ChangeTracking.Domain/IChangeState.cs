namespace Lab.ChangeTracking.Domain;

public interface IChangeState
{
    ChangeState EntityState { get; init; }

    CommitState CommitState { get; init; }

    int Version { get; set; }
}