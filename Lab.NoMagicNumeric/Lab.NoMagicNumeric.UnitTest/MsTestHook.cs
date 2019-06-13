using System;
using System.Collections.Generic;
using Lab.NoMagicNumeric.EntityModel.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.NoMagicNumeric.UnitTest
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
                    dbContext.Database.Delete();
                }
            }
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            using (var dbContext = new LabDbContext())
            {
                var toDb = new List<Order>
                {
                    new Order {Id = Guid.NewGuid(), IsTransform = "Y", Status = "99"},
                    new Order {Id = Guid.NewGuid(), IsTransform = "N", Status = "10"}
                };
                dbContext.Orders.AddRange(toDb);
                dbContext.SaveChanges();
            }
        }

    }
}
