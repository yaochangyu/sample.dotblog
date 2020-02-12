using System;
using System.Data.Entity.SqlServer;
using System.IO;
using Lab.EF6.SqliteCodeFirst.UnitTest.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EF6.SqliteCodeFirst.UnitTest
{
    [TestClass]
    public class MsTestHook
    {
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            using (var dbContext = new LabDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    //dbContext.Database.Delete();
                }
            }
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            AppDomain.CurrentDomain.SetData("DataDirectory", currentDirectory);
            using (var dbContext = new LabDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    //dbContext.Database.Delete();
                }
            }
        }
    }
}