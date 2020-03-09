using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestCore.Logging;

namespace UnitTestCore
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
