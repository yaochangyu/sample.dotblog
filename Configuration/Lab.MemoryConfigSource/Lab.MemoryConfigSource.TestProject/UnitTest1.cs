using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.MemoryConfigSource.TestProject;

public enum Gender
{
    Male,
    Female
}

public class Member
{
    public string Id { get; set; }

    public Profile Profile { get; set; }
}

public class Profile
{
    public Gender Gender { get; set; }

    public int Age { get; set; }

    public string Address { get; set; }
}

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void 綁定複雜型別()
    {
        var source = new Dictionary<string, string>
        {
            ["id"] = "9527",
            ["profile:gender"] = "Male",
            ["profile:age"] = "18",
            ["profile:address"] = "Taipei",
        };
        var builder = new ConfigurationBuilder();
        var configRoot = builder.AddInMemoryCollection(source).Build();
        var member = configRoot.Get<Member>();
        
        Assert.AreEqual("9527", member.Id);
        Assert.AreEqual(18, member.Profile.Age);
        Assert.AreEqual("Taipei", member.Profile.Address);
        Assert.AreEqual(Gender.Male, member.Profile.Gender);
    }
}