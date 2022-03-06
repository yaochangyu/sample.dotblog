using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using EFCore.BulkExtensions;
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

    private static readonly IDbContextFactory<EmployeeDbContext> s_employeeDbContextFactory =
        TestAssistants.EmployeeDbContextFactory;

    private readonly IEmployeeAggregate _employeeAggregate = TestAssistants.EmployeeAggregate;

    private readonly IEmployeeRepository _employeeRepository = TestAssistants.EmployeeRepository;

    private readonly ISystemClock _systemClock = TestAssistants.SystemClock;

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

    [TestMethod]
    public void 刪除一筆資料()
    {
        var fromDb = Insert();
        var employeeEntity = new EmployeeEntity();
        employeeEntity.AsTrackable(fromDb)
            .SetDelete()
            .AcceptChanges(this._systemClock, _accessContext, _uuIdProvider);

        var count = this._employeeRepository.SaveChangeAsync(employeeEntity).Result;

        Assert.AreEqual(1, count);
        var dbContext = s_employeeDbContextFactory.CreateDbContext();

        var actual = dbContext.Employees
                .Where(p => p.Id == fromDb.Id)
                .Include(p => p.Identity)
                .Include(p => p.Addresses)
                .FirstOrDefault()
            ;
        Assert.AreEqual(null, actual);
    }

    [TestMethod]
    public void 更新一筆資料()
    {
        var fromDb = Insert();
        var employeeEntity = new EmployeeEntity();
        employeeEntity.AsTrackable(fromDb)
            .SetProfile("小章", 19, "我變了")
            .AcceptChanges(this._systemClock, _accessContext, _uuIdProvider);

        var count = this._employeeRepository.SaveChangeAsync(employeeEntity).Result;

        Assert.AreEqual(1, count);
        var dbContext = s_employeeDbContextFactory.CreateDbContext();

        var actual = dbContext.Employees
                .Where(p => p.Id == fromDb.Id)
                .Include(p => p.Identity)
                .Include(p => p.Addresses)
                .First()
            ;
        Assert.AreEqual("小章", actual.Name);
        Assert.AreEqual(19, actual.Age);
        Assert.AreEqual("我變了", actual.Remark);
    }

    [TestMethod]
    public void 沒有異動()
    {
        var fromDb = Insert();
        var employeeEntity = new EmployeeEntity();
        employeeEntity.AsTrackable(fromDb)
            .SetProfile("小章", 19, "新來的")
            .SetProfile("yao", 18, "編輯")
            .AcceptChanges(this._systemClock, _accessContext, _uuIdProvider);

        var count = this._employeeRepository.SaveChangeAsync(employeeEntity).Result;
        Assert.AreEqual(0, count);
    }

    [TestMethod]
    public void 新增一筆資料()
    {
        var employeeEntity = new EmployeeEntity();
        employeeEntity.New("yao", 10, "新的")
            .AcceptChanges(this._systemClock, _accessContext, _uuIdProvider);

        var count = this._employeeRepository.SaveChangeAsync(employeeEntity).Result;

        Assert.AreEqual(1, count);
        var dbContext = s_employeeDbContextFactory.CreateDbContext();

        var actual = dbContext.Employees
                .Where(p => p.Id == employeeEntity.Id)
                .Include(p => p.Identity)
                .Include(p => p.Addresses)
                .First()
            ;
        Assert.AreEqual("yao", actual.Name);
        Assert.AreEqual(10, actual.Age);
        Assert.AreEqual("新的", actual.Remark);
    }

    private static void DeleteAllTable()
    {
        var dbContext = s_employeeDbContextFactory.CreateDbContext();
        dbContext.Employees.BatchDelete();
        dbContext.Addresses.BatchDelete();
        dbContext.Identity.BatchDelete();
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
            Remark = "編輯",
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