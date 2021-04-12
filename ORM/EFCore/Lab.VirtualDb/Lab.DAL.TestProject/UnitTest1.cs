using Lab.DAL.DomainModel.Employee;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DAL.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        public void 操作真實資料庫()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host    = builder.Build();
            var repository = host.Services.GetService<EmployeeRepository>();
            var count      = repository.InsertAsync(new InsertRequest(), "").Result;
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void 操作記憶體()
        {
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host = builder.Build();

            DefaultDbContextFactory.SetUseMemoryDatabase();
            var repository = host.Services.GetService<EmployeeRepository>();
            var count      = repository.InsertAsync(new InsertRequest(), "").Result;
            Assert.AreEqual(1, count);
        }
    }
}