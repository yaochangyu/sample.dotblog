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
            var options = DbContextOptionManager.CreateEmployeeDbContextOptions();
            using (var dbContext = new EmployeeContext(options))
            {
                var employees = dbContext.Employees.AsNoTracking().ToList();
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            var options = DbContextOptionManager.CreateEmployeeDbContextOptions();

            using (var dbContext = new EmployeeContext(options))
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