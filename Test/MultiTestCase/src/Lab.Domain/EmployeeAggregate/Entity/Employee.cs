namespace Lab.Domain.Entity;

public record Employee
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public int? Age { get; set; }

    public string Remark { get; set; }

    public DateTimeOffset CreateAt { get; set; }

    public string CreateBy { get; set; }

    public Employee SetName(string name)
    {
        this.Name = name;
        return this;
    }

    public Employee SetAge(int age)
    {
        this.Age = age;
        return this;
    }
    
}