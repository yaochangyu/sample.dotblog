namespace Lab.ChangeTracking.Domain;

public record ProfileEntity
{
    public virtual string FirstName { get; set; }

    public virtual string LastName { get; set; }
}