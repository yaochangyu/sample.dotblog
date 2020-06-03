using System;
using System.Linq;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DAL.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            using (var dbContext = new LabEmployeeContext(DbOptionsFactory.DbContextOptions))
            {
                var employees = dbContext.Employees.AsNoTracking().ToList();
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            using (var dbContext = new LabEmployeeContext(DbOptionsFactory.DbContextOptions))
            {
                var id = Guid.NewGuid();
                var toDb = new Employee
                {
                    Id   = id,
                    Name = "yao",
                    Age  = 18,
                };
                dbContext.Employees.Add(toDb);
                var count = dbContext.SaveChanges();
                Assert.AreEqual(true, count           != 0);
                Assert.AreEqual(true, toDb.SequenceId != 0);
            }
        }
    }
}