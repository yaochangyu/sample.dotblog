using System;
using System.IO;
using System.Linq;
using Lab.DAL.DomainModel.Employee;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.DAL.TestProject
{
    [TestClass]
    public class EmployeeRepositoryUnitTests
    {
        private static readonly DbContextOptions<EmployeeDbContext> s_employeeContextOptions;
        public static           string                              TestDbConnectionString;

        static EmployeeRepositoryUnitTests()
        {
            s_employeeContextOptions = CreateDbContextOptions();

            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json");

            var configRoot = configBuilder.Build();
            TestDbConnectionString = configRoot.GetConnectionString("DefaultConnection");
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            //刪除測試資料庫
            Console.WriteLine("AssemblyCleanup");

            using var db = new TestEmployeeDbContext(TestDbConnectionString);
            db.Database.EnsureDeleted();
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            //刪除測試資料庫
            Console.WriteLine("AssemblyInitialize");
            using var db = new TestEmployeeDbContext(TestDbConnectionString);
            db.Database.EnsureDeleted();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            //刪除測試資料表
            Console.WriteLine("ClassCleanup");
            DeleteTestDataRow();
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            //刪除測試資料表
            Console.WriteLine("ClassInitialize");
            DeleteTestDataRow();
        }

        [TestMethod]
        public void 操作真實資料庫_手動取得Repository執行個體()
        {
            //arrange
            DefaultDbContextManager.Now = new DateTime(1900, 1, 1);
            DefaultDbContextManager.SetPhysicalDatabase<EmployeeDbContext>();

            var repository = new EmployeeRepository();

            // var id         = Guid.NewGuid();
            repository.NewAsync(new NewRequest
            {
                Account  = "yao",
                Password = "123456",
                Name     = "余小章",
                Age      = 18,
                Remark   = "測試案例，持續航向偉大航道"
            }, "TestUser").Wait();
            using var db = new EmployeeDbContext(s_employeeContextOptions);
            var       id = db.Employees.FirstOrDefault(p => p.Name == "余小章").Id;

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

            using var db1 = new TestEmployeeDbContext(TestDbConnectionString);

            var actual = db1.OrderHistories
                            .AsNoTracking()
                            .FirstOrDefault();

            Assert.AreEqual("A001", actual.Product_Id);
            Assert.AreEqual("羅技滑鼠", actual.Product_Name);
        }

        [TestMethod]
        public void 操作真實資料庫_注入EmployeeDbContext()
        {
            //arrange
            DefaultDbContextManager.Now = new DateTime(1900, 1, 1);

            var connectionString =
                "Server=(localdb)\\mssqllocaldb;Database=Lab.DAL.Injection;Trusted_Connection=True;MultipleActiveResultSets=true";

            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services =>
                                                 {
                                                     services.AddSingleton<EmployeeRepository>();
                                                     services.AddDbContext<EmployeeDbContext>(builder =>
                                                     {
                                                         builder.UseSqlServer(connectionString);
                                                     });
                                                 });
            var host = builder.Build();

            var dbContextOptions  = host.Services.GetService<DbContextOptions<EmployeeDbContext>>();
            var repository        = host.Services.GetService<EmployeeRepository>();
            var employeeDbContext = host.Services.GetService<EmployeeDbContext>();
            employeeDbContext.Database.EnsureCreated();

            repository.EmployeeDbContext = employeeDbContext;

            //act
            var count = repository.NewAsync(new NewRequest
            {
                Account  = "yao",
                Password = "123456",
                Name     = "余小章",
                Age      = 18,
            }, "TestUser").Result;

            //assert
            Assert.AreEqual(2, count);
            using var db = new EmployeeDbContext(dbContextOptions);

            var actual = db.Employees
                           .Include(p => p.Identity)
                           .AsNoTracking()
                           .FirstOrDefault();

            Assert.AreEqual(actual.Name,              "余小章");
            Assert.AreEqual(actual.Age,               18);
            Assert.AreEqual(actual.Identity.Account,  "yao");
            Assert.AreEqual(actual.Identity.Password, "123456");
            db.Database.EnsureDeleted();
        }

        [TestMethod]
        public void 操作真實資料庫_預設EmployeeDbContext()
        {
            //arrange
            DefaultDbContextManager.Now = new DateTime(1900, 1, 1);
            DefaultDbContextManager.SetPhysicalDatabase<EmployeeDbContext>();

            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host = builder.Build();

            var repository = host.Services.GetService<EmployeeRepository>();

            //act
            var count = repository.NewAsync(new NewRequest
            {
                Account  = "yao",
                Password = "123456",
                Name     = "余小章",
                Age      = 18,
            }, "TestUser").Result;

            //assert
            Assert.AreEqual(2, count);

            using var db = new TestEmployeeDbContext(TestDbConnectionString);

            var actual = db.Employees
                           .Include(p => p.Identity)
                           .AsNoTracking()
                           .FirstOrDefault();

            Assert.AreEqual(actual.Name,              "余小章");
            Assert.AreEqual(actual.Age,               18);
            Assert.AreEqual(actual.Identity.Account,  "yao");
            Assert.AreEqual(actual.Identity.Password, "123456");
        }

        [TestMethod]
        public void 操作記憶體()
        {
            DefaultDbContextManager.Now = new DateTime(1900, 1, 1);
            DefaultDbContextManager.SetMemoryDatabase<EmployeeDbContext>();

            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services => { services.AddSingleton<EmployeeRepository>(); });
            var host = builder.Build();

            var repository = host.Services.GetService<EmployeeRepository>();
            var count      = repository.NewAsync(new NewRequest(), "TestUser").Result;
            Assert.AreEqual(2, count);
        }

        private static DbContextOptions<EmployeeDbContext> CreateDbContextOptions()
        {
            var configBuilder = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json");

            var configRoot       = configBuilder.Build();
            var connectionString = configRoot.GetConnectionString("DefaultConnection");

            var loggerFactory = LoggerFactory.Create(builder =>
                                                     {
                                                         builder

                                                             //.AddFilter("Microsoft",                 LogLevel.Warning)
                                                             //.AddFilter("System",                    LogLevel.Warning)
                                                             .AddFilter("Lab.DAL", LogLevel.Debug)
                                                             .AddConsole()
                                                             ;
                                                     });
            return new DbContextOptionsBuilder<EmployeeDbContext>()
                   .UseSqlServer(connectionString)
                   .UseLoggerFactory(loggerFactory)
                   .Options;
        }

        private static void DeleteTestDataRow()
        {
            using var db = new TestEmployeeDbContext(TestDbConnectionString);
            if (db.Database.CanConnect() == false)
            {
                return;
            }

            var deleteCommand = GetDeleteAllRecordCommand();
            db.Database.ExecuteSqlRaw(deleteCommand);
        }

        private static string GetDeleteAllRecordCommand()
        {
            var sql = @"
-- disable referential integrity
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL' 


EXEC sp_MSForEachTable 'DELETE FROM ?' 


-- enable referential integrity again 
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL' 
";

            return sql;
        }
    }
}