using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EnvFileConfig.TestProject;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void 讀取ENV檔案()
    {
        var configRoot = new ConfigurationBuilder()

                         // .AddJsonFile("appSettings.json")
                         .AddEnvFile("secret.env")
                         .Build()
            ;
        var section = configRoot.GetSection("SQL_SERVER_CS");
        Console.WriteLine($"Value = {section.Value}");
    }

    [TestMethod]
    public void 讀取ENV檔案後綁定()
    {
        var configRoot = new ConfigurationBuilder()
                         .AddEnvFile("secret.env")
                         .Build()
            ;
        var appSetting = configRoot.Get<AppSetting>();
        Assert.AreEqual("foo-bar", appSetting.SQL_SERVER_CS);
        Assert.AreEqual("localhost:6379", appSetting.REDIS_ENDPOINT);
    }
}