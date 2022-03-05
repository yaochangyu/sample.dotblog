namespace Lab.ChangeTracking.Domain;

public enum EntityState
{
    Unchanged = 0,
    Added = 1,
    Modified = 2,
    Submitted = 99,
}