using System.ComponentModel;

namespace Lab.ChangeTracking.Domain;

public interface IChangeTime
{
    DateTimeOffset? CreatedAt { get; init; }

    string? CreatedBy { get; init; }

    DateTimeOffset? ModifiedAt { get; init; }

    string? ModifiedBy { get; init; }
}