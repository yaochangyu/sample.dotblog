using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
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
        // CleanData();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        CleanData();
    }

    [TestMethod]
    public void TestMethod2()
    {
        var host = CreateHostBuilder(null).Start();
        host.Services.GetService<IDbContextFactory<EmployeeDbContext>>();
    }

    [TestMethod]
    public void 查詢所有資料()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var newEmployee = Insert();
        using var db = TestAssistant.EmployeeDbContextFactory.CreateDbContext();
        var actual = db.Employees
            .Where(p => p.Id == newEmployee.Id)
            .Select(p => new
            {
                Profiles = p.Profiles.To<Dictionary<string, object>>(options),
            })
            .FirstOrDefault();
    }

    [TestMethod]
    public void 查詢特定欄位_JsonDoc()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var newEmployee = Insert();
        using var db = TestAssistant.EmployeeDbContextFactory.CreateDbContext();
        var actual = db.Employees

            // .Where(p => p.Profiles.RootElement.GetProperty("long").GetString() == "255")
            .Where(p => p.Profiles.RootElement.GetProperty("long").GetInt64() == 255)
            .Select(p => new
            {
                Profiles = p.Profiles.To<Dictionary<string, object>>(options),
            })
            .FirstOrDefault();
    }

    [TestMethod]
    public void 查詢特定欄位_POCO()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var newEmployee = Insert();
        using var db = TestAssistant.EmployeeDbContextFactory.CreateDbContext();
        var actual = db.Employees
                .Where(p => p.Customer.Age > 12)
                .Select(p => new
                {
                    p.Customer

                    // Order = p.Customer.Orders.Select(p => new { p.Price, p.ShippingAddress })
                    // Order = p.Customer
                    //     .Orders
                    //     .Select(p => new Order
                    //     {
                    //         Price = p.Price
                    //     })
                    //     

                    // aa = p.Customer.Orders.ToDictionary(p => p.Price, p => p.ShippingAddress)
                })

                // .AsAsyncEnumerable()
                .FirstOrDefault()
            ;
    }

    [TestMethod]
    public void 新增資料()
    {
        using var db = TestAssistant.EmployeeDbContextFactory.CreateDbContext();
        var expected = new Dictionary<string, object>
        {
            ["anonymousType"] = new { Prop = 123 },
            ["model"] = new Model { Age = 19, Name = "yao" },
            ["null"] = null!,
            ["dateTimeOffset"] = DateTimeOffset.Now,
            ["long"] = (long)255,
            ["decimal"] = (decimal)3.1416,
            ["guid"] = Guid.NewGuid(),
            ["string"] = "String",
            ["decimalArray"] = new[] { 1, (decimal)2.1 },
        };

        var id = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = id,
            Age = RandomNumber.Next(1, 100),
            Name = Name.FullName(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Sys",
            Profiles = expected.ToJsonDocument(),
        });

        db.SaveChanges();
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

    private static Employee Insert()
    {
        using var db = TestAssistant.EmployeeDbContextFactory.CreateDbContext();
        var expected = new Dictionary<string, object>
        {
            ["anonymousType"] = new { Prop = 123 },
            ["model"] = new Model { Age = 19, Name = "yao" },
            ["null"] = null!,
            ["dateTimeOffset"] = DateTimeOffset.Now,
            ["long"] = (long)255,
            ["decimal"] = (decimal)3.1416,
            ["guid"] = Guid.NewGuid(),
            ["string"] = "String",
            ["decimalArray"] = new[] { 1, (decimal)2.1 },
        };

        var id = Guid.NewGuid();
        var newEmployee = new Employee
        {
            Id = id,
            Age = RandomNumber.Next(1, 100),
            Name = Name.FullName(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "Sys",
            Profiles = expected.ToJsonDocument(),
            Customer = new Customer
            {
                Age = 19,
                Name = "小章",
                Orders = new[]
                {
                    new Order
                    {
                        Price = (decimal)22.1,
                        ShippingAddress = "台北市"
                    }
                },
                Product = new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Mouse"
                }
            }
        };
        db.Employees.Add(newEmployee);

        db.SaveChanges();
        return newEmployee;
    }
}

public record Model
{
    public int Age { get; set; }

    public string Name { get; set; }
}