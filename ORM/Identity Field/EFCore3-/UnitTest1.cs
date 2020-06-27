using System;
using EFCore3.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFCore3
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod2()
        {
            using (var dbContext = new LabContext(DbOptionsFactory.DbContextOptions))
            {
                var id = Guid.NewGuid();
                var toDb = new Member
                {
                    Id   = id,
                    Name = "yao",
                    Age  = 18,
                };
                dbContext.Members.Add(toDb);
                var count = dbContext.SaveChanges();
                Assert.AreEqual(true, count           != 0);
                Assert.AreEqual(true, toDb.SequenceId != 0);
            }
        }
    }
}