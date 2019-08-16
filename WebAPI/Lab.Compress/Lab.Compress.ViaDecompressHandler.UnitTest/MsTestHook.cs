using System;
using System.Net.Http;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.Compress.ViaDecompressHandler.UnitTest
{
    /// <summary>
    ///     Summary description for UnitTest2
    /// </summary>
    [TestClass]
    public class MsTestHook
    {
        private const  string      HOST_ADDRESS = "http://localhost:9529";
        private static IDisposable s_webApp;
        public static HttpClient  Client;

        /// <summary>
        ///     Gets or sets the test context which provides
        ///     information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

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
            Client             = new HttpClient();
            Client.BaseAddress = new Uri(HOST_ADDRESS);
        }

        #region Additional test attributes

        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        #endregion
    }
}