using System;
using System.Linq;
using Lab.DAL.DomainModel.Employee;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DAL.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var options = DefaultDbContextManager.CreateEmployeeDbContextOptions();
            using (var dbContext = new EmployeeContext(options))
            {
                var employees = dbContext.Employees.AsNoTracking().ToList();
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            var options = DefaultDbContextManager.CreateEmployeeDbContextOptions();

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

        [TestMethod]
        public void 注入DbContextFactor操作真實資料庫()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(service =>
                                                 {
                                                     service
                                                         .AddDbContextFactory<EmployeeContext>(DefaultDbContextManager
                                                             .ApplyConfigurePhysical);
                                                     service.AddSingleton<EmployeeRepository>();
                                                 });
            var host       = builder.Build();
            var repository = host.Services.GetService<EmployeeRepository>();
            var count      = repository.InsertAsync(new InsertRequest(), "").Result;
            Assert.AreEqual(1, count);
        }
    }
}