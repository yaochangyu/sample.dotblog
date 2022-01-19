using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ConfigBind.TestProject;

[TestClass]
public class 來源為環境變數
{
    [TestMethod]
    public void 綁定字典()
    {
        Environment.SetEnvironmentVariable("a:id", "9527");
        Environment.SetEnvironmentVariable("a:profile:gender", "Male");
        Environment.SetEnvironmentVariable("a:profile:age", "18");
        Environment.SetEnvironmentVariable("a:profile:address", "Taipei");
        Environment.SetEnvironmentVariable("b:id", "9528");
        Environment.SetEnvironmentVariable("b:profile:gender", "Male");
        Environment.SetEnvironmentVariable("b:profile:age", "19");
        Environment.SetEnvironmentVariable("b:profile:address", "Taipei");
        var builder = new ConfigurationBuilder();
        var configRoot = builder.AddInMemoryCollection()
                                .Build();
        var member = configRoot.Get<Dictionary<string, Member>>();
    }

    [TestMethod]
    public void 綁定集合()
    {
        Environment.SetEnvironmentVariable("a:id", "9527");
        Environment.SetEnvironmentVariable("a:profile:gender", "Male");
        Environment.SetEnvironmentVariable("a:profile:age", "18");
        Environment.SetEnvironmentVariable("a:profile:address", "Taipei");
        Environment.SetEnvironmentVariable("b:id", "9528");
        Environment.SetEnvironmentVariable("b:profile:gender", "Male");
        Environment.SetEnvironmentVariable("b:profile:age", "19");
        Environment.SetEnvironmentVariable("b:profile:address", "Taipei");
        var builder = new ConfigurationBuilder();
        var configRoot = builder.AddInMemoryCollection()
                                .Build();
        var member = configRoot.Get<IList<Member>>();
        var member2 = configRoot.Get<Dictionary<string, Member>>();

        Assert.AreEqual("9527", member[0].Id);
        Assert.AreEqual("9528", member[1].Id);
    }

    [TestMethod]
    public void 綁定複雜型別()
    {
        Environment.SetEnvironmentVariable("id", "9527");
        Environment.SetEnvironmentVariable("profile:gender", "Male");
        Environment.SetEnvironmentVariable("profile:age", "18");
        Environment.SetEnvironmentVariable("profile:address", "Taipei");

        var builder = new ConfigurationBuilder();
        builder.AddEnvironmentVariables();
        var configRoot = builder.AddInMemoryCollection()
                                .Build();
        var member = configRoot.Get<Member>();
        Assert.AreEqual("9527", member.Id);
        Assert.AreEqual(18, member.Profile.Age);
        Assert.AreEqual("Taipei", member.Profile.Address);
        Assert.AreEqual(Gender.Male, member.Profile.Gender);
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