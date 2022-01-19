using System;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EnvFileConfig.TestProject;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
        var configRoot = new ConfigurationBuilder()

                         // .AddJsonFile("appSettings.json")
                         .AddEnvFile("secret.env")
                         .Build()
            ;
        var section = configRoot.GetSection("SQL_SERVER_CS");
        Console.WriteLine($"Value = {section.Value}");
    }
}