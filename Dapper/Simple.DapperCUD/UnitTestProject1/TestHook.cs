using System;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.EntityModel;

namespace UnitTestProject1
{
    [TestClass]
    public class TestHook
    {
        //[AssemblyInitialize]
        public static void AssemblyInitializeAttachFile(TestContext context)
        {
            var instance = SqlProviderServices.Instance;
            var currentDirectory = Directory.GetCurrentDirectory();
            AppDomain.CurrentDomain.SetData("DataDirectory", currentDirectory);
            using (var dbContext = new TestDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }
            }

        }

        //[AssemblyCleanup]
        public static void AssemblyCleanupAttachFile()
        {
            using (var dbContext = new TestDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }
            }
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            var instance = SqlProviderServices.Instance;
            Database.SetInitializer(new DropCreateDatabaseAlways<TestDbContext>());

            //Database.SetInitializer(new TestDropCreateDatabaseAlways());

            using (var dbContext = new TestDbContext())
            {
                if (dbContext.Database.Exists())
                {
                    dbContext.Database.Delete();
                }
                dbContext.Database.Initialize(true);
            }
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            //using (var dbContext = new TestDbContext())
            //{
            //    if (dbContext.Database.Exists())
            //    {
            //        dbContext.Database.Delete();
            //    }
            //}
        }
    }

}
