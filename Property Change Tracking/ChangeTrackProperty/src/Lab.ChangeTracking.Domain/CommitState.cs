namespace Lab.ChangeTracking.Domain;

public enum CommitState
{
    Unchanged = 0,
    Rejected = 1,
    Accepted = 99,
}