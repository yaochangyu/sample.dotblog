using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EF6.SqliteCodeFirstNet4.DAL.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestCleanup]
        public void After()
        {
            using (var db = new LabDbContext())
            {
                //db.Database.ExecuteSqlCommand("delete from Identity;");

                //db.Database.ExecuteSqlCommand("delete from Employee;");
            }
        }

        [TestInitialize]
        public void Before()
        {
            using (var db = new LabDbContext())
            {
                //db.Database.ExecuteSqlCommand("delete from Identity;");

                //db.Database.ExecuteSqlCommand("delete from Employee;");
            }
        }
        [TestMethod]
        public void Insert()
        {
            var toDb = new Employee
            {
                Id   = Guid.NewGuid(),
                Name = "yao3",
                Age = 20
            };
            using (var db = new LabDbContext())
            {
                db.Employees.Add(toDb);
                var count = db.SaveChanges();
                Assert.AreEqual(1, count);
            }
        }
    }
}
