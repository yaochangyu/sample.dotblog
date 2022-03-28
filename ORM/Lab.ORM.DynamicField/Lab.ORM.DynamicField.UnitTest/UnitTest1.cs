using System;
using System.Collections.Generic;
using System.Linq;
using EFCore.BulkExtensions;
using Faker;
using Lab.ORM.DynamicField.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ORM.DynamicField.UnitTest;

[TestClass]
public class UnitTest1
{
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
        using var db = TestAssistant.EmployeeDbContextFactory.CreateDbContext();

        // var id = Guid.NewGuid();
        // db.Employees.Add(new Employee
        // {
        //     Id = id,
        //     Age = RandomNumber.Next(1, 100),
        //     Name = Name.FullName(),
        //     CreatedAt = DateTimeOffset.UtcNow,
        //     CreatedBy = "Sys",
        //
        //     // Identity = new Identity
        //     // {
        //     //     Account = "yao",
        //     //     CreateAt = DateTimeOffset.UtcNow,
        //     //     CreateBy = "Sys",
        //     //     Password = "123456",
        //     // },
        // });
        var generateEmployees = GenerateEmployees(1);
        db.AddRange(generateEmployees);
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
        using var dbContext = TestAssistant.EmployeeDbContextFactory.CreateDbContext();
        dbContext.Employees.BatchDelete();
    }

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostBuilder, services) => { TestAssistant.ConfigureTestServices(services); });
    }

    private static List<Employee> GenerateEmployees(int totalCount)
    {
        var employees = Enumerable.Range(0, totalCount)
            .Select((x, i) =>
            {
                var now = DateTimeOffset.UtcNow;
                var sysAccount = "sys";
                return new Employee
                {
                    Id = Guid.NewGuid(),
                    Age = RandomNumber.Next(1, 100),
                    Name = Name.FullName(),
                    CreatedBy = sysAccount,
                    CreatedAt = now,
                    ModifiedAt = null,
                    ModifiedBy = null,

                    // Name = Name.First(),
                };
            }).ToList();
        return employees;
    }
}