using BenchmarkDotNet.Running;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject2
{
    [TestClass]
    public class BenchmarkRunnerTests
    {
        [TestMethod]
        public void OrmTest()
        {
            var summary = BenchmarkRunner.Run<OrmBenchmark>();
        }
    }
}