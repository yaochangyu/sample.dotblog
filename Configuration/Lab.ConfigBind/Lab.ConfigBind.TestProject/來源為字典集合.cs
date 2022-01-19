using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ConfigBind.TestProject;

[TestClass]
public class 來源為字典集合
{
    [TestMethod]
    public void 綁定集合()
    {
        var source = new Dictionary<string, string>
        {
            ["a:id"] = "9527",
            ["a:profile:gender"] = "Male",
            ["a:profile:age"] = "18",
            ["a:profile:address"] = "Taipei",
            ["b:id"] = "9528",
            ["b:profile:gender"] = "Male",
            ["b:profile:age"] = "19",
            ["b:profile:address"] = "Taipei",
        };

        var builder = new ConfigurationBuilder();
        var configRoot = builder.AddInMemoryCollection(source).Build();
        var member = configRoot.Get<IList<Member>>();

        Assert.AreEqual("9527", member[0].Id);
        Assert.AreEqual("9528", member[1].Id);
    }

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

    [TestMethod]
    public void 綁定環境變數()
    {
        var source = new Dictionary<string, string>
        {
            ["a:id"] = "9527",
            ["a:profile:gender"] = "Male",
            ["a:profile:age"] = "18",
            ["a:profile:address"] = "Taipei",
            ["b:id"] = "9528",
            ["b:profile:gender"] = "Male",
            ["b:profile:age"] = "19",
            ["b:profile:address"] = "Taipei",
        };
        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        var configRoot = builder.AddInMemoryCollection(source).Build();
        var member = configRoot.Get<IList<Member>>();

        Assert.AreEqual("9527", member[0].Id);
        Assert.AreEqual("9528", member[1].Id);
    }

    private enum Gender
    {
        Male,
        Female
    }

    private class Member
    {
        public string Id { get; set; }

        public Profile Profile { get; set; }
    }

    private class Profile
    {
        public Gender? Gender { get; set; }

        public int? Age { get; set; }

        public string Address { get; set; }
    }
}