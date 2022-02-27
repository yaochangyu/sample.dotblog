namespace Lab.ChangeTracking.Abstract;

public interface IChangeTime
{
    DateTimeOffset CreatedAt { get; set; }

    string CreatedBy { get; set; }

    DateTimeOffset UpdatedAt { get; set; }

    string UpdatedBy { get; set; }
}