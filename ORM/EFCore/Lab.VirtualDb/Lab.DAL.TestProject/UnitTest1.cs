using System;
using Lab.DAL.DomainModel.Employee;
using Lab.DAL.EntityModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DAL.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        
        [TestMethod]
        public void 操作真實資料庫()
        {
            DefaultDbContextManager.Now = new DateTime(1900, 1, 1);

            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host       = builder.Build();
            var repository = host.Services.GetService<EmployeeRepository>();
            var count = repository.NewAsync(new NewRequest
            {
                Id       = Guid.NewGuid(),
                Account  = "yao",
                Password = "123456",
                Name     = "余小章",
                Age      = 18,
                Remark   = "測試案例，持續航向偉大航道"
            }, "TestUser").Result;
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void 操作真實資料庫1()
        {
            //arrange
            DefaultDbContextManager.Now = new DateTime(1900, 1, 1);

            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host       = builder.Build();
            var repository = host.Services.GetService<EmployeeRepository>();
            var id         = Guid.NewGuid();
            repository.NewAsync(new NewRequest
            {
                Id       = id,
                Account  = "yao",
                Password = "123456",
                Name     = "余小章",
                Age      = 18,
                Remark   = "測試案例，持續航向偉大航道"
            }, "TestUser").Wait();

            //act
            var count = repository.InsertLogAsync(new InsertOrderRequest
            {
                Employee_Id  = id,
                Product_Id   = "A001",
                Product_Name = "羅技滑鼠",
                Remark       = "測試案例，持續航向偉大航道"
            }, "TestUser").Result;

            //assert
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void 操作記憶體()
        {
            DefaultDbContextManager.Now = new DateTime(1900, 1, 1);
            DefaultDbContextManager.SetUseMemoryDatabase<EmployeeContext>();

            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host = builder.Build();

            var repository = host.Services.GetService<EmployeeRepository>();
            var count      = repository.NewAsync(new NewRequest(), "TestUser").Result;
            Assert.AreEqual(2, count);
        }
    }
}