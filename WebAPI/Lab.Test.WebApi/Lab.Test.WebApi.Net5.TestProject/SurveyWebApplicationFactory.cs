using System;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.Test.WebApi.Net5.TestProject
{
    [TestClass]
    public class SurveyWebApplicationFactory
    {
        [TestMethod]
        public void CustomTestServer()
        {
            var server = new CustomTestServer();
            var httpClient = server.CreateClient();
            var url = "demo";
            var response = httpClient.GetAsync(url).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(result);
        }

        [TestMethod]
        public void WebApplicationFactory基本用法()
        {
            var server = new WebApplicationFactory<Startup>();
            var client = server.CreateClient();
            var url = "demo";
            var response = client.GetAsync(url).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(result);
        }
    }
}