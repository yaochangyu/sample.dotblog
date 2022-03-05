using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ChangeTracking;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ChangeTracking.Domain.UnitTest;

[TestClass]
public class ChangeTrackingUnitTest
{
    private readonly IEmployeeAggregate _employeeAggregate = TestAssistants.EmployeeAggregate;

    private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory =
        TestAssistants.EmployeeDbContextFactory;

    private readonly IEmployeeRepository _employeeRepository = TestAssistants.EmployeeRepository;

    [TestMethod]
    public void 異動追蹤後存檔()
    {
        var toDB = Insert().To();
        var trackable = toDB.AsTrackable();
        trackable.Age = 20;
        trackable.Name = "小章";
        trackable.Identity.Remark = "我變了";
        trackable.Addresses[0].Remark = "我變了";
        trackable.Addresses.RemoveAt(1);
        trackable.Addresses.Add(new AddressEntity()
        {
            Id = Guid.NewGuid(),
            Employee_Id = toDB.Id,
            CreatedAt = DateTimeOffset.Now,
            CreatedBy = "sys",
            Country = "Taipei",
            Street = "Street",
            Remark = "我新的"
        });
        var employeeEntity = this._employeeRepository.SaveChangeAsync(trackable).Result;
    }

    [TestMethod]
    public void 異動追蹤後存檔_回傳不可變的物件()
    {
        var toDB = Insert();
        var source = new EmployeeEntity
        {
            Id = toDB.Id,
            Name = "yao",
            Age = 12,
            Identity = new IdentityEntity
            {
                Employee_Id = toDB.Identity.Employee_Id
            },
            Addresses = new List<AddressEntity>
            {
                new()
                {
                    Id = toDB.Addresses[0]
                        .Id,
                    Employee_Id = toDB.Id,
                    Remark = "AAA"
                },
                new()
                {
                    Id = toDB.Addresses[1]
                        .Id,
                    Employee_Id = toDB.Id,
                    Remark = "AAA"
                }
            }
        };
        var employeeEntity = this._employeeAggregate.ModifyFlowAsync(source).Result;
        this.DataShouldOk(source);
    }

    private void DataShouldOk(EmployeeEntity source)
    {
        var dbContext = this._employeeDbContextFactory.CreateDbContext();
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