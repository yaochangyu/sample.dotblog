using System;
using EFCore3.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFCore3
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void InsertViaEFCore3()
        {
            using (var dbContext = new LabDbContext(DbOptionsFactory.DbContextOptions))
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

        [TestCleanup]
        public void TestCleanup()
        {
            this.DeleteAll();
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.DeleteAll();
        }

        private void DeleteAll()
        {
            var sql = @"
-- disable referential integrity
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL' 


EXEC sp_MSForEachTable 'DELETE FROM ?' 


-- enable referential integrity again 
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL' 
";
            using (var dbContext = new LabDbContext(DbOptionsFactory.DbContextOptions))
            {
                dbContext.Database.ExecuteSqlCommand(sql);
            }
        }
    }
}