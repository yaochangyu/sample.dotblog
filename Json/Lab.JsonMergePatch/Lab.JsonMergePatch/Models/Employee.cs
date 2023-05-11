namespace Lab.JsonMergePatch.Models;

public class Employee
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public Address? Address { get; set; }

    public DateTime? Birthday { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public List<string> Extensions { get; set; } = new();
}