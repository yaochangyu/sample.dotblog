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
            var id = Guid.NewGuid();
            var toDb = new Member
            {
                Id   = id,
                Name = "yao",
                Age  = 18,
            };
            Console.WriteLine($"寫入資料庫之前 {nameof(toDb.SequenceId)} = {toDb.SequenceId}");
            using (var dbContext = new LabDbContext(DbOptionsFactory.DbContextOptions))
            {
                dbContext.Members.Add(toDb);
                var count = dbContext.SaveChanges();
                Assert.AreEqual(true, count           != 0);
                Assert.AreEqual(true, toDb.SequenceId != 0);
                Console.WriteLine($"寫入資料庫之後 {nameof(toDb.SequenceId)} = {toDb.SequenceId}");
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
TRUNCATE TABLE Member
";

            using (var dbContext = new LabDbContext(DbOptionsFactory.DbContextOptions))
            {
                dbContext.Database.ExecuteSqlCommand(sql);
            }
        }
    }
}