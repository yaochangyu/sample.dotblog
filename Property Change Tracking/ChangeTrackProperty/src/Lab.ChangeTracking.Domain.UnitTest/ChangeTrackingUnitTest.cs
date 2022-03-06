using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Lab.MultiTestCase.UnitTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ChangeTracking.Domain.UnitTest;

[TestClass]
public class ChangeTrackingUnitTest
{
    private static readonly IAccessContext _accessContext = TestAssistants.AccessContext;

    private static readonly IUUIdProvider _uuIdProvider = TestAssistants.UUIdProvider;
    private readonly IEmployeeAggregate _employeeAggregate = TestAssistants.EmployeeAggregate;

    private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory =
        TestAssistants.EmployeeDbContextFactory;

    private readonly IEmployeeRepository _employeeRepository = TestAssistants.EmployeeRepository;

    private readonly ISystemClock _systemClock = TestAssistants.SystemClock;

    [TestMethod]
    public void 異動追蹤後存檔()
    {
        var employeeEntity = Insert().To();
        employeeEntity.AsTrackable();
        employeeEntity.SetProfile("小章", 19,"新來的");
        employeeEntity.AcceptChanges(this._systemClock, _accessContext, _uuIdProvider);

        var count = this._employeeRepository.SaveChangeAsync(employeeEntity).Result;
        var dbContext = this._employeeDbContextFactory.CreateDbContext();

        var actual = dbContext.Employees
                .Where(p => p.Id == employeeEntity.Id)
                .Include(p => p.Identity)
                .Include(p => p.Addresses)
                .First()
            ;
        Assert.AreEqual("小章", actual.Name);
        Assert.AreEqual(19, actual.Age);
        Assert.AreEqual("新來的", actual.Remark);
        Assert.AreEqual("我新的", actual.Addresses[1].Remark);
    }

    [TestMethod]
    public void 新增一筆後存檔()
    {
        var employeeEntity = new EmployeeEntity();
        employeeEntity.New("yao", 10);
        employeeEntity.AcceptChanges(this._systemClock, _accessContext, _uuIdProvider);

        var count = this._employeeRepository.SaveChangeAsync(employeeEntity).Result;
        var dbContext = this._employeeDbContextFactory.CreateDbContext();

        var actual = dbContext.Employees
                .Where(p => p.Id == employeeEntity.Id)
                .Include(p => p.Identity)
                .Include(p => p.Addresses)
                .First()
            ;
        Assert.AreEqual("我變了", actual.Remark);
        Assert.AreEqual("我變了", actual.Identity.Remark);
        Assert.AreEqual("我變了", actual.Addresses[0].Remark);
        Assert.AreEqual("我新的", actual.Addresses[1].Remark);
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