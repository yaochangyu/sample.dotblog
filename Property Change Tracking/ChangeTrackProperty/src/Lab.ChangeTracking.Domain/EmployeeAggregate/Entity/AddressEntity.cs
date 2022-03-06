namespace Lab.ChangeTracking.Domain;

public record AddressEntity
{
    public Guid Id { get; set; }

    public Guid Employee_Id { get; set; }

    public string Country { get; set; }

    public string Street { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string ModifiedBy { get; set; }

    public string Remark { get; set; }
}