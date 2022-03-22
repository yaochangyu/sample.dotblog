namespace Lab.ChangeTracking.Domain;

public enum CommitState
{
    Unchanged = 0,
    Accepted = 1,
    Rejected = 2,
    Commited = 3
}