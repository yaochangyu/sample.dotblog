using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.Logging;

//using UnitTestProject1.Logging;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var logger = LogProvider.GetCurrentClassLogger();
            logger.Debug("Debug");
        }
    }
}