namespace Lab.ChangeTracking.Domain;

public enum EntityState
{
    Added = 0,
    Modified = 1,
    Submitted = 2,
    Unchanged = 99,
}