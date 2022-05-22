using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.AspNetCore.Security.BasicAuthenticationSite.IntegrateTest;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void 訪問沒有授權的服務()
    {
        var server = new TestServer();
        var httpClient = server.CreateClient();
        var url = "demo";
        var response = httpClient.GetAsync(url).Result;
        var result = response.Content.ReadAsStringAsync().Result;
        Console.WriteLine(result);
    }
}