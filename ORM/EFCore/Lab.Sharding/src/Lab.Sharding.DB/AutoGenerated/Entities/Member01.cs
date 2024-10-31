using System;
using System.Collections.Generic;

namespace Lab.Sharding.DB;

public partial class Member01
{
    public string Id { get; set; } = null!;

    public string? Name { get; set; }

    public int? Age { get; set; }

    public long SequenceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? ChangedAt { get; set; }

    public string? ChangedBy { get; set; }

    public string? Email { get; set; }
}
