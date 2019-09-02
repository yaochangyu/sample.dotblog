using System;
using System.Net.Http;
using Autofac;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Server.UnitTest.Controllers;
using Server.UnitTest.Repositories;

namespace Server.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        private const  string      HOST_ADDRESS = "http://localhost:9527";
        private static IDisposable s_webApp;
        private static HttpClient  s_client;

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            s_webApp.Dispose();
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            s_webApp = WebApp.Start<Startup>(HOST_ADDRESS);
            Console.WriteLine("Web API started!");
            s_client             = new HttpClient();
            s_client.BaseAddress = new Uri(HOST_ADDRESS);
            Console.WriteLine("HttpClient started!");
        }

        [TestMethod]
        public void Given_ChangeInstance_When_Call_Get_Should_Be_FakeRepository()
        {
            var fakeRepository = Substitute.For<IProductRepository>();
            fakeRepository.GetName().Returns("Fake Repository");

            SetProductRepository(fakeRepository);

            var url      = "api/Default";
            var response = s_client.GetAsync(url).Result;
            var result   = response.Content.ReadAsAsync<string>().Result;
            Assert.AreEqual("Fake Repository", result);
        }

        [TestMethod]
        public void When_Call_Get_Should_Be_Product2()
        {
            var url      = "api/Default";
            var response = s_client.GetAsync(url).Result;
            var result   = response.Content.ReadAsAsync<string>().Result;
            Assert.AreEqual("Product2", result);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            var autofacManager = Startup.AutofacManager;
            var builder        = autofacManager.CreateApiBuilder();
            var container = autofacManager.CreateContainer(builder);
        }

        private static void SetDefaultController(IProductRepository repository)
        {
            var autofacManager    = Startup.AutofacManager;
            var defaultController = autofacManager.GetController<DefaultController>();

            defaultController.ProductRepository = repository;

            var builder = autofacManager.CreateApiBuilder();
            autofacManager.SetController(defaultController);
            autofacManager.CreateContainer(builder);
        }

        private static void SetProductRepository(IProductRepository repository)
        {
            var autofacManager = Startup.AutofacManager;
            var builder        = autofacManager.CreateApiBuilder();
            builder.RegisterInstance(repository).As<IProductRepository>();
            autofacManager.CreateContainer(builder);
        }
    }
}