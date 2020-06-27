using System;
using EF6.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EF6
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void InsertViaEF6()
        {
            var toDb = new Member
            {
                Id   = Guid.NewGuid(),
                Name = "yao",
                Age  = 20
            };

            Console.WriteLine($"寫入資料庫之前 {nameof(toDb.SequenceId)} = {toDb.SequenceId}");

            using (var dbContext = LabDbContext.Create())
            {
                dbContext.Database.Log = Console.WriteLine;
                dbContext.Members.Add(toDb);
                dbContext.SaveChanges();
            }

            Console.WriteLine($"寫入資料庫之後 {nameof(toDb.SequenceId)} = {toDb.SequenceId}");
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
            using (var dbContext = LabDbContext.Create())
            {
                dbContext.Database.ExecuteSqlCommand(sql);
            }
        }
    }
}