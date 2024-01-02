namespace Lab.SpecflowCreateAndCompareJson;

public class Member
{
    public string Id { get; set; }

    public int Age { get; set; }

    public Name Name { get; set; }

    public State State { get; set; }

    public List<string> IpData { get; set; }

    public List<Order> Orders { get; set; }
}

public enum State
{
    None,
    Active,
    Inactive,
}

public class Order
{
    public string Id { get; set; }
}

public class Name
{
    public string FirstName { get; set; }

    public string LastName { get; set; }
}