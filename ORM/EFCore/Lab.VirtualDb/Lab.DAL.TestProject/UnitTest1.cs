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
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host       = builder.Build();
            var repository = host.Services.GetService<EmployeeRepository>();
            var count = repository.InsertAsync(new InsertRequest
            {
                Id       = Guid.NewGuid(),
                Account  = "yao",
                Password = "123456",
                Name     = "余小章",
                Age      = 18,
                Remark   = "測試案例，持續航向偉大航道"
            }, "").Result;
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public void 操作記憶體()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host = builder.Build();

            DefaultDbContextManager.SetUseMemoryDatabase<EmployeeContext>();
            var repository = host.Services.GetService<EmployeeRepository>();
            var count      = repository.InsertAsync(new InsertRequest(), "").Result;
            Assert.AreEqual(2, count);
        }
    }
}