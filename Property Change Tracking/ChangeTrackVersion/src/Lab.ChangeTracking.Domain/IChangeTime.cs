using System.ComponentModel;

namespace Lab.ChangeTracking.Domain;

public interface IChangeTime
{
    public Guid Id { get; set; }

    DateTimeOffset CreatedAt { get; set; }

    string CreatedBy { get; set; }

    DateTimeOffset? ModifiedAt { get; set; }

    string? ModifiedBy { get; set; }
}