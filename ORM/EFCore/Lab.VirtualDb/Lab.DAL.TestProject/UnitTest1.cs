<<<<<<< HEAD
using Lab.DAL.DomainModel.Employee;
=======
using System;
using System.Linq;
using Lab.DAL.DomainModel.Employee;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
>>>>>>> origin/master
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DAL.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
<<<<<<< HEAD
        public void 操作真實資料庫()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
=======
        public void TestMethod1()
        {
            var options = DefaultDbContextFactory.CreateEmployeeDbContextOptions();
            using (var dbContext = new EmployeeContext(options))
            {
                var employees = dbContext.Employees.AsNoTracking().ToList();
            }
        }

        [TestMethod]
        public void TestMethod2()
        {
            var options = DefaultDbContextFactory.CreateEmployeeDbContextOptions();

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
        public void 注入DbContextFactory操作真實資料庫()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services =>
                                                 {
                                                     services.AddSingleton<EmployeeRepository>();
                                                 });
>>>>>>> origin/master
            var host       = builder.Build();
            var repository = host.Services.GetService<EmployeeRepository>();
            var count      = repository.InsertAsync(new InsertRequest(), "").Result;
            Assert.AreEqual(1, count);
        }

        [TestMethod]
<<<<<<< HEAD
        public void 操作記憶體()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host = builder.Build();

            DefaultDbContextFactory.SetUseMemoryDatabase();

=======
        public void 注入DbContextFactory操作記憶體()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services =>
                                                 {
                                                     services.AddSingleton<EmployeeRepository>();
                                                 });
            var host       = builder.Build();
            
            DefaultDbContextFactory.UseMemory();
            
>>>>>>> origin/master
            var repository = host.Services.GetService<EmployeeRepository>();
            var count      = repository.InsertAsync(new InsertRequest(), "").Result;
            Assert.AreEqual(1, count);
        }
    }
}