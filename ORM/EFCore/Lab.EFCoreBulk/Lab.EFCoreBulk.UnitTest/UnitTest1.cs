using System;
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
        var totalCount = 1000000;
        var toDb = Enumerable.Range(0, totalCount)
                                  .Select(x => new Employee
                                  {
                                      Id = Guid.NewGuid(),

                                      // Id = Guid.NewGuid(),
                                      Age = 10,

                                      // Age = RandomNumber.Next(1, 100),
                                      CreateBy = "yao",

                                      // CreateBy = Name.FullName(),
                                      CreateAt = DateTimeOffset.Now,

                                      // CreateAt = DateTimeOffset.Now,
                                      Name = "yao"

                                      // Name = Name.First(),
                                  }).ToList();

        var watch = new Stopwatch();
        watch.Restart();

        db.AddRange(toDb);

        // db.BulkInsert(employees);

        watch.Stop();

        var count = db.Employees.Count();
        Console.WriteLine($"資料庫存在筆數={count},共花費={watch.Elapsed}");
    }

    [TestMethod]
    public void BulkInsert()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var totalCount = 1000000;
        var employees = Enumerable.Range(0, totalCount)
                                  .Select(x => new Employee
                                  {
                                      Id = Guid.NewGuid(),

                                      // Id = Guid.NewGuid(),
                                      Age = 10,

                                      // Age = RandomNumber.Next(1, 100),
                                      CreateBy = "yao",

                                      // CreateBy = Name.FullName(),
                                      CreateAt = DateTimeOffset.Now,

                                      // CreateAt = DateTimeOffset.Now,
                                      Name = "yao"

                                      // Name = Name.First(),
                                  }).ToList();

        var config = new BulkConfig { SetOutputIdentity = false, BatchSize = 4000, UseTempDB = true };
        var watch = new Stopwatch();
        watch.Restart();

        db.BulkInsert(employees, config);

        // db.BulkInsert(employees);

        watch.Stop();

        var count = db.Employees.Count();
        Console.WriteLine($"資料庫存在筆數={count},共花費={watch.Elapsed}");
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

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
                   .ConfigureServices((hostBuilder, services) =>
                   {
                       TestInstanceManager.ConfigureTestServices(services);
                   });
    }
}