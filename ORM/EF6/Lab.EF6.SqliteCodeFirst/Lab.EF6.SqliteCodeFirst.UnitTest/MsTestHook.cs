using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EF6.SqliteCodeFirst.UnitTest
{
    [TestClass]
    public class MsTestHook
    {
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            DeleteDb();
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            DeleteDb();
        }

        private static void DeleteDb()
        {
            var filePath = "lab.db";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}