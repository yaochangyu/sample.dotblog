using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ChangeTracking;
using EFCore.BulkExtensions;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ChangeTracking.Domain.UnitTest;

[TestClass]
public class ChangeTrackingUnitTest
{
    private readonly IEmployeeAggregate _employeeAggregate = TestAssistants.EmployeeAggregate;

    private static readonly IDbContextFactory<EmployeeDbContext> s_employeeDbContextFactory =
        TestAssistants.EmployeeDbContextFactory;

    private readonly IEmployeeRepository _employeeRepository = TestAssistants.EmployeeRepository;

    [TestMethod]
    public void 異動追蹤後存檔()
    {
        var employeeEntity = Insert().To();
        var trackable = employeeEntity.AsTrackable();
        trackable.Age = 20;
        trackable.Name = "小章";
        trackable.Remark = "我變了";
        trackable.Identity.Remark = "我變了";
        trackable.Addresses[0].Remark = "我變了";
        trackable.Addresses.RemoveAt(1);
        trackable.Addresses.Add(new AddressEntity()
        {
            Id = Guid.NewGuid(),
            Employee_Id = employeeEntity.Id,
            CreatedAt = DateTimeOffset.Now,
            CreatedBy = "sys",
            Country = "Taipei",
            Street = "Street",
            Remark = "我新的"
        });
        var count = this._employeeRepository.SaveChangeAsync(trackable).Result;
        var dbContext = s_employeeDbContextFactory.CreateDbContext();
        var actual = dbContext.Employees
                .Where(p => p.Id == employeeEntity.Id)
                .Include(p => p.Identity)
                .Include(p => p.Addresses)
                .First()
            ;
        Assert.AreEqual("我變了",actual.Remark);
        Assert.AreEqual("我變了",actual.Identity.Remark);
        Assert.AreEqual("我變了",actual.Addresses[0].Remark);
        Assert.AreEqual("我新的",actual.Addresses[1].Remark);
    }

    [TestMethod]
    public void 異動追蹤後存檔_回傳不可變的物件()
    {
        // var toDB = Insert();
        // var source = new EmployeeEntity
        // {
        //     Id = toDB.Id,
        //     Name = "yao",
        //     Age = 12,
        //     Identity = new IdentityEntity
        //     {
        //         Employee_Id = toDB.Identity.Employee_Id
        //     },
        //     Addresses = new List<AddressEntity>
        //     {
        //         new()
        //         {
        //             Id = toDB.Addresses[0]
        //                 .Id,
        //             Employee_Id = toDB.Id,
        //             Remark = "AAA"
        //         },
        //         new()
        //         {
        //             Id = toDB.Addresses[1]
        //                 .Id,
        //             Employee_Id = toDB.Id,
        //             Remark = "AAA"
        //         }
        //     }
        // };
        // var employeeEntity = this._employeeAggregate.ModifyFlowAsync(source).Result;
        // this.DataShouldOk(source);
    }

    private void DataShouldOk(EmployeeEntity source)
    {
        var dbContext = s_employeeDbContextFactory.CreateDbContext();
        var actual = dbContext.Employees
                .Where(p => p.Id == source.Id)
                .Include(p => p.Identity)
                .First()
            ;

        Assert.AreEqual("小章", actual.Name);
        Assert.AreEqual("9527", actual.Identity.Password);
    }

    private static Employee Insert()
    {
        using var dbContext = TestAssistants.EmployeeDbContextFactory.CreateDbContext();
        var employeeId = Guid.NewGuid();
        var toDB = new Employee
        {
            Id = employeeId,
            Age = 18,
            Name = "yao",
            CreatedAt = DateTimeOffset.Now,
            CreatedBy = "Sys",
            Identity = new Identity
            {
                Employee_Id = employeeId,
                Account = "yao",
                Password = "123456",
                CreatedAt = DateTimeOffset.Now,
                CreatedBy = "Sys",
                Remark = "編輯"
            },
            Addresses = new List<Address>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Employee_Id = employeeId,
                    CreatedAt = DateTimeOffset.Now,
                    CreatedBy = "sys",
                    Country = "Taipei",
                    Street = "Street",
                    Remark = "修改的"
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Employee_Id = employeeId,
                    CreatedAt = DateTimeOffset.Now,
                    CreatedBy = "sys",
                    Country = "Taipei",
                    Street = "Street",
                    Remark = "刪除的"
                }
            }
        };
        dbContext.Employees.Add(toDB);
        dbContext.SaveChanges();
        return toDB;
    }
    [ClassCleanup]
    public static void ClassCleanup()
    {
        DeleteAllTable();
    }

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        DeleteAllTable();
    }
    private static void DeleteAllTable()
    {
        var dbContext = s_employeeDbContextFactory.CreateDbContext();
        dbContext.Employees.BatchDelete();
        dbContext.Addresses.BatchDelete();
        dbContext.Identity.BatchDelete();
    }
    private static string ToJson<T>(T instance)
    {
        var serialize = JsonSerializer.Serialize(instance,
                                                 new JsonSerializerOptions
                                                 {
                                                     Encoder = JavaScriptEncoder.Create(
                                                         UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs)
                                                 });
        return serialize;
    }
}