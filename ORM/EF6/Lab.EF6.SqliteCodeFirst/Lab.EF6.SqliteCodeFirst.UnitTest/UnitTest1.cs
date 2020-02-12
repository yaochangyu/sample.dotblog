using System;
using System.Data.Entity;
using System.Linq;
using Lab.EF6.SqliteCodeFirst.UnitTest.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EF6.SqliteCodeFirst.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestCleanup]
        public void After()
        {
            using (var db = new LabDbContext())
            {
                db.Database.ExecuteSqlCommand("delete from Identity;");
                db.Database.ExecuteSqlCommand("delete from Employee;");
            }
        }

        [TestInitialize]
        public void Before()
        {
            using (var db = new LabDbContext())
            {
                db.Database.ExecuteSqlCommand("delete from Identity;");
                db.Database.ExecuteSqlCommand("delete from Employee;");
            }
        }

        [TestMethod]
        public void Insert()
        {
            var toDb = new Employee
            {
                Id   = Guid.NewGuid(),
                Name = "yao",
                Age  = 18
            };
            using (var db = new LabDbContext())
            {
                db.Employees.Add(toDb);
                var count = db.SaveChanges();
                Assert.AreEqual(1, count);
            }
        }

        [TestMethod]
        public void InsertTwoTable()
        {
            var toDb = new Employee
            {
                Id       = Guid.NewGuid(),
                Name     = "yao",
                Age      = 18,
                Identity = new Identity {Account = "yao", Password = "123456"}
            };
            using (var db = new LabDbContext())
            {
                db.Employees.Add(toDb);
                var count = db.SaveChanges();
                Assert.AreEqual(2, count);
            }
        }

        [TestMethod]
        public void Query_GUID_Type()
        {
            var id = Guid.NewGuid();
            var toDb = new Employee
            {
                Id   = id,
                Name = "yao",
                Age  = 18
            };
            using (var db = new LabDbContext())
            {
                db.Employees.Add(toDb);
                db.SaveChanges();
            }

            using (var db = new LabDbContext())
            {
                var employee = db.Employees
                                 .Where(p => p.Id == id)
                                 .AsNoTracking()
                                 .FirstOrDefault();
                Assert.IsNull(employee);
            }
        }
    }
}