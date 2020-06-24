using System;
using EF6.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EF6
{
    [TestClass]
    public class MsTestHook
    {
        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            using (var dbContext = LabDbContext.Create())
            {
                var isExist = dbContext.Database.Exists();
                if (isExist)
                {
                    //dbContext.Database.Delete();
                }
            }
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            using (var dbContext = LabDbContext.Create())
            {
                var isExist = dbContext.Database.Exists();
                if (isExist)
                {
                    //dbContext.Database.Delete();
                }

                //dbContext.Database.Initialize(true);
            }
        }
    }
}