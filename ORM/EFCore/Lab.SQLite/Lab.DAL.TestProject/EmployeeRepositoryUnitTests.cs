using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lab.DAL.DomainModel.Employee;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
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
        private static readonly string TestDbConnectionString1 = "Data Source=Lab.DAL.TestProject.db";
        private static readonly string TestDbConnectionString2 = "Data Source=Lab.DAL.Injection.db";

        static EmployeeRepositoryUnitTests()
        {
            s_employeeContextOptions = CreateDbContextOptions();
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            //刪除測試資料庫
            Console.WriteLine("AssemblyCleanup");

            using var db1 = new TestEmployeeDbContext(TestDbConnectionString1);
            db1.Database.EnsureDeleted();

            using var db2 = new TestEmployeeDbContext(TestDbConnectionString2);
            db2.Database.EnsureDeleted();
        }

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            //刪除測試資料庫
            Console.WriteLine("AssemblyInitialize");
            using var db1 = new TestEmployeeDbContext(TestDbConnectionString1);
            db1.Database.EnsureDeleted();

            using var db2 = new TestEmployeeDbContext(TestDbConnectionString2);
            db2.Database.EnsureDeleted();

            // //建立測試資料庫
            // db.Database.Migrate();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            //刪除測試資料表
            Console.WriteLine("ClassCleanup");

            // DeleteTestDataRow();
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            //刪除測試資料表
            Console.WriteLine("ClassInitialize");

            // DeleteTestDataRow();
        }

        [TestMethod]
        public void 操作真實資料庫_手動取得Repository執行個體()
        {
            //arrange
            DefaultDbContextManager.Now = new DateTime(1900, 1, 1);
            DefaultDbContextManager.SetPhysicalDatabase<EmployeeDbContext>();

            var repository = new EmployeeRepository();

            repository.NewAsync(new NewRequest
            {
                Account  = "yao",
                Password = "123456",
                Name     = "余小章",
                Age      = 18,
                Remark   = "測試案例，持續航向偉大航道"
            }, "TestUser").Wait();

            using var db = new TestEmployeeDbContext(TestDbConnectionString1);
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
        }

        [TestMethod]
        public void 操作真實資料庫_手動實例化EmployeeDbContext()
        {
            var       contextOptions = CreateDbContextOptions();
            using var dbContext      = new EmployeeDbContext(contextOptions);
            var       id             = Guid.NewGuid().ToString();
            dbContext.Employees.Add(new Employee()
            {
                Age      = 18,
                Id       = id,
                CreateAt = DateTime.Now,
                CreateBy = "test",
                Name     = "yao"
            });
            dbContext.SaveChanges();

            var actual = dbContext.Employees.AsNoTracking().FirstOrDefault(p => p.Id == id);
            Assert.AreEqual(18,    actual.Age);
            Assert.AreEqual("yao", actual.Name);
        }

        [TestMethod]
        public void 操作真實資料庫_由Host註冊EmployeeDbContext()
        {
            //arrange
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services =>
                                                 {
                                                     services.AddDbContext<EmployeeDbContext>(
                                                      (provider, builder) =>
                                                      {
                                                          var config =
                                                              provider.GetService<IConfiguration>();
                                                          var connectionString =
                                                              config.GetConnectionString("DefaultConnection");
                                                          var loggerFactory = provider.GetService<ILoggerFactory>();
                                                          builder.UseSqlite(connectionString)
                                                                 .UseLoggerFactory(loggerFactory)
                                                              ;
                                                      });
                                                 });
            var host = builder.Build();

            var       dbContextOptions = host.Services.GetService<DbContextOptions<EmployeeDbContext>>();
            using var dbContext        = host.Services.GetService<EmployeeDbContext>();

            //act
            var id  = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            dbContext.Employees.Add(new Employee()
            {
                Id       = id,
                Name     = "余小章",
                Age      = 18,
                CreateAt = now,
                CreateBy = "test user"
            });
            dbContext.Identities.Add(new Identity()
            {
                Employee_Id = id,
                Account     = "yao",
                Password    = "123456",
                CreateAt    = now,
                CreateBy    = "test user"
            });
            var count = dbContext.SaveChanges();

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
        }

        [TestMethod]
        public void 操作真實資料庫_由Host註冊EmployeeDbContextPool()
        {
            //arrange
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services =>
                                                 {
                                                     services.AddDbContextPool<EmployeeDbContext>(
                                                      (provider, builder) =>
                                                      {
                                                          var config =
                                                              provider.GetService<IConfiguration>();
                                                          var connectionString =
                                                              config.GetConnectionString("DefaultConnection");
                                                          var loggerFactory = provider.GetService<ILoggerFactory>();
                                                          builder.UseSqlite(connectionString)
                                                                 .UseLoggerFactory(loggerFactory)
                                                              ;
                                                      }, 64);
                                                 });
            var host = builder.Build();

            var       dbContextOptions = host.Services.GetService<DbContextOptions<EmployeeDbContext>>();
            using var dbContext        = host.Services.GetService<EmployeeDbContext>();

            //act
            var id  = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            dbContext.Employees.Add(new Employee()
            {
                Id       = id,
                Name     = "余小章",
                Age      = 18,
                CreateAt = now,
                CreateBy = "test user"
            });
            dbContext.Identities.Add(new Identity()
            {
                Employee_Id = id,
                Account     = "yao",
                Password    = "123456",
                CreateAt    = now,
                CreateBy    = "test user"
            });
            var count = dbContext.SaveChanges();

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
        }

        [TestMethod]
        public void 操作真實資料庫_由Host註冊EmployeeDbContextFactory()
        {
            //arrange
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services =>
                                                 {
                                                     services.AddDbContextFactory<EmployeeDbContext>(
                                                      (provider, builder) =>
                                                      {
                                                          var config =
                                                              provider.GetService<IConfiguration>();
                                                          var connectionString =
                                                              config.GetConnectionString("DefaultConnection");
                                                          var loggerFactory = provider.GetService<ILoggerFactory>();
                                                          builder.UseSqlite(connectionString)
                                                                 .UseLoggerFactory(loggerFactory)
                                                              ;
                                                      });
                                                 });
            var host = builder.Build();

            var       dbContextOptions = host.Services.GetService<DbContextOptions<EmployeeDbContext>>();
            var       dbContextFactory = host.Services.GetService<IDbContextFactory<EmployeeDbContext>>();
            using var dbContext        = dbContextFactory.CreateDbContext();

            //act
            var id  = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            dbContext.Employees.Add(new Employee()
            {
                Id       = id,
                Name     = "余小章",
                Age      = 18,
                CreateAt = now,
                CreateBy = "test user"
            });
            dbContext.Identities.Add(new Identity()
            {
                Employee_Id = id,
                Account     = "yao",
                Password    = "123456",
                CreateAt    = now,
                CreateBy    = "test user"
            });
            var count = dbContext.SaveChanges();

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
        }

        [TestMethod]
        public void 操作真實資料庫_由Host註冊EmployeeDbContextPoolFactory()
        {
            //arrange
            var builder = Host.CreateDefaultBuilder()
                              .ConfigureServices(services =>
                                                 {
                                                     services.AddPooledDbContextFactory<EmployeeDbContext>(
                                                      (provider, builder) =>
                                                      {
                                                          var config =
                                                              provider.GetService<IConfiguration>();
                                                          var connectionString =
                                                              config.GetConnectionString("DefaultConnection");
                                                          var loggerFactory = provider.GetService<ILoggerFactory>();
                                                          builder.UseSqlite(connectionString)
                                                                 .UseLoggerFactory(loggerFactory)
                                                              ;
                                                      }, 64);
                                                 });
            var host = builder.Build();

            var       dbContextOptions = host.Services.GetService<DbContextOptions<EmployeeDbContext>>();
            var       dbContextFactory = host.Services.GetService<IDbContextFactory<EmployeeDbContext>>();
            using var dbContext        = dbContextFactory.CreateDbContext();

            //act
            var id  = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            dbContext.Employees.Add(new Employee()
            {
                Id       = id,
                Name     = "余小章",
                Age      = 18,
                CreateAt = now,
                CreateBy = "test user"
            });
            dbContext.Identities.Add(new Identity()
            {
                Employee_Id = id,
                Account     = "yao",
                Password    = "123456",
                CreateAt    = now,
                CreateBy    = "test user"
            });
            var count = dbContext.SaveChanges();

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
            using var db = new TestEmployeeDbContext(TestDbConnectionString1);

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
                   .UseSqlite(connectionString)
                   .UseLoggerFactory(loggerFactory)
                   .Options;
        }

        private static void DeleteTestDataRow()
        {
            var       dbContextOptions = s_employeeContextOptions;
            using var db               = new EmployeeDbContext(dbContextOptions);
            var       deleteCommand    = GetDeleteAllRecordCommand();
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