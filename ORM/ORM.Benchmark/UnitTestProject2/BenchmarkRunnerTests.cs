using BenchmarkDotNet.Running;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject2
{
    [TestClass]
    public class BenchmarkRunnerTests
    {
        [TestMethod]
        public void BenchmarkRunnerTest()
        {
            var summary = BenchmarkRunner.Run<OrmBenchmark>();
        }
    }
}