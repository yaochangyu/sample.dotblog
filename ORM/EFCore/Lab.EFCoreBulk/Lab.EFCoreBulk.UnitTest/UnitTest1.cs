using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EFCore.BulkExtensions;
using Lab.EFCoreBulk.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EFCoreBulk.UnitTest;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void AddRanges()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var toDb = GetEmployees(1000000);
        var watch = new Stopwatch();
        watch.Restart();

        db.AddRange(toDb);
        var changeCount = db.SaveChanges();

        watch.Stop();

        var count = db.Employees.Count();
        Console.WriteLine($"資料庫存在筆數={count}，共花費={watch.Elapsed}");
    }

    [TestMethod]
    public void BatchDelete()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var toDb = GetEmployees(10000);
        var update = new Employee
        {
            Id = Guid.NewGuid(),
            Age = 10,
            CreateBy = "yao",
            CreateAt = DateTimeOffset.Now,
            Name = "yao",
            Remark = "等待更新"
        };
        toDb.Add(update);
        var config = new BulkConfig { SetOutputIdentity = false, BatchSize = 4000, UseTempDB = true };
        db.BulkInsert(toDb, config);

        var watch = new Stopwatch();
        watch.Restart();

        db.Employees
          .Where(p => p.Id == update.Id)
          .BatchDelete();

        watch.Stop();

        var count = db.Employees.Count();
        var isExist = db.Employees.Any(p => p.Id == update.Id);
        Assert.AreEqual(false, isExist);
        Console.WriteLine($"資料庫存在筆數={count},共花費={watch.Elapsed},{update.Id} 資料不存在");
    }

    [TestMethod]
    public void BatchUpdate()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var toDb = GetEmployees(10000);
        var update = new Employee
        {
            Id = Guid.NewGuid(),
            Age = 10,
            CreateBy = "yao",
            CreateAt = DateTimeOffset.Now,
            Name = "yao",
            Remark = "等待更新"
        };
        toDb.Add(update);
        var config = new BulkConfig { SetOutputIdentity = false, BatchSize = 4000, UseTempDB = true };
        db.BulkInsert(toDb, config);

        var watch = new Stopwatch();
        watch.Restart();

        db.Employees
          .Where(p => p.Id == update.Id)
          .BatchUpdate(new Employee { Remark = "Updated" });

        watch.Stop();

        var count = db.Employees.Count();
        Console.WriteLine($"資料庫存在筆數={count},共花費={watch.Elapsed}");
    }

    [TestMethod]
    public void BulkInsert()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var toDb = GetEmployees(1000000);

        var config = new BulkConfig { SetOutputIdentity = false, BatchSize = 4000, UseTempDB = true };

        var watch = new Stopwatch();
        watch.Restart();

        db.BulkInsert(toDb, config);

        watch.Stop();

        var count = db.Employees.Count();
        Console.WriteLine($"資料庫存在筆數={count},共花費={watch.Elapsed}");
    }

    [TestMethod]
    public void BulkRead()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var toDb = GetEmployees(100);
        {
            var config = new BulkConfig { SetOutputIdentity = false, BatchSize = 4000, UseTempDB = true };
            db.BulkInsert(toDb, config);
        }

        var watch = new Stopwatch();
        watch.Restart();
        {
            var items = new List<Employee>
            {
                new() { Name = "yao1" },
                new() { Name = "yao2" }
            };
            var config = new BulkConfig
            {
                UpdateByProperties = new List<string>
                {
                    nameof(Employee.Name),
                },
                UseTempDB = true
            };
            db.BulkRead(items, config);
        }

        watch.Stop();

        Console.WriteLine($"共花費={watch.Elapsed}");
    }

    [TestMethod]
    public void BulkSaveChanges()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var toDb = GetEmployees(1000);

        db.AddRange(toDb);

        var config = new BulkConfig
        {
            PropertiesToExclude = new List<string> { "SequenceId" },
            BulkCopyTimeout = 30,
            BatchSize = 4000,
            UseTempDB = true
        };

        var watch = new Stopwatch();
        watch.Restart();

        db.BulkSaveChanges(config);

        watch.Stop();

        var count = db.Employees.Count();
        Console.WriteLine($"資料庫存在筆數={count},共花費={watch.Elapsed}");
    }

    [TestMethod]
    public void Contains()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var toDb = GetEmployees(100);
        {
            var config = new BulkConfig { SetOutputIdentity = false, BatchSize = 4000, UseTempDB = true };
            db.BulkInsert(toDb, config);
        }

        var watch = new Stopwatch();
        watch.Restart();

        var items = new List<string> { "yao1", "yao2" };
        var employees = db.Employees.Where(a => items.Contains(a.Name)).AsNoTracking().ToList(); //SQL IN operator

        watch.Stop();

        Console.WriteLine($"共花費={watch.Elapsed}");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        CleanData();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        CleanData();
    }

    [TestMethod]
    public void TestMethod1()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var id = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = id,
            Age = 18,
            CreateAt = DateTimeOffset.UtcNow,
            Name = "yao",
            CreateBy = "Sys",

            // Identity = new Identity
            // {
            //     Account = "yao",
            //     CreateAt = DateTimeOffset.UtcNow,
            //     CreateBy = "Sys",
            //     Password = "123456",
            // },
        });
        db.SaveChanges();
    }

    [TestMethod]
    public void TestMethod2()
    {
        var host = CreateHostBuilder(null).Start();
        host.Services.GetService<IDbContextFactory<EmployeeDbContext>>();
    }

    private static void CleanData()
    {
        using var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();

        // db.Truncate<OrderHistory>();
        // db.Truncate<Identity>();
        using var transaction = db.Database.BeginTransaction();

        db.OrderHistories
          .BatchDelete();

        db.Identities
          .BatchDelete();

        // db.Truncate<Employee>();
        db.Employees
          .BatchDelete();

        transaction.Commit();

        // db.Employees
        //   .Where(p => p.Id != Guid.Empty)
        //   .BatchDelete();
        //
        // while (db.Employees.Any())
        // {
        //     var deletedCount = db.Employees
        //                          .Where(p => p.Id != Guid.Empty)
        //                          .Take(1000000)
        //                          .BatchDelete();
        //     var count = db.Employees.Count();
        //     Console.WriteLine($"已刪除 {deletedCount} 筆，剩下 {count} 筆");
        // }
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                   .ConfigureServices((hostBuilder, services) =>
                   {
                       TestInstanceManager.ConfigureTestServices(services);
                   });
    }

    private static List<Employee> GetEmployees(int totalCount)
    {
        var employees = Enumerable.Range(0, totalCount)
                                  .Select((x, i) => new Employee
                                  {
                                      Id = Guid.NewGuid(),

                                      // Id = Guid.NewGuid(),
                                      Age = 10,

                                      // Age = RandomNumber.Next(1, 100),
                                      CreateBy = "yao",

                                      // CreateBy = Name.FullName(),
                                      CreateAt = DateTimeOffset.Now,

                                      // CreateAt = DateTimeOffset.Now,
                                      Name = $"yao{i}"

                                      // Name = Name.First(),
                                  }).ToList();
        return employees;
    }
}