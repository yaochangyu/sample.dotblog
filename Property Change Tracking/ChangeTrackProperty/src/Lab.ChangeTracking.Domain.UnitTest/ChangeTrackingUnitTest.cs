using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Lab.MultiTestCase.UnitTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Employee = Lab.ChangeTracking.Infrastructure.DB.EntityModel.Employee;

namespace Lab.ChangeTracking.Domain.UnitTest;

[TestClass]
public class ChangeTrackingUnitTest
{
    private readonly IEmployeeAggregate _employeeAggregate = TestAssistants.EmployeeAggregate;

    private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory =
        TestAssistants.EmployeeDbContextFactory;

    private ISystemClock _systemClock = TestAssistants.SystemClock;

    public static IAccessContext _accessContext = TestAssistants.AccessContext;

    public static IUUIdProvider _uuIdProvider = TestAssistants.UUIdProvider;

    [TestMethod]
    public void 追蹤()
    {
        var employeeEntity = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            State = EntityState.Added,
            Name = "yao",
            Age = 19,
            Remark = "remark"
        };
        employeeEntity.InitialTrack();
        employeeEntity.SetProfile("小章", 20,"remark");
        employeeEntity.SetProfile("小章", 20,"remark");
    }

    [TestMethod]
    public void 追蹤集合()
    {
    }

    private static Employee Insert()
    {
        using var dbContext = TestAssistants.EmployeeDbContextFactory.CreateDbContext();
        var toDB = new Employee
        {
            Id = Guid.NewGuid(),
            Age = 18,
            Name = "yao",
            CreateAt = DateTimeOffset.Now,
            CreateBy = "TEST",
            Identity = new Identity
            {
                Account = "yao",
                Password = "123456",
                CreateAt = DateTimeOffset.Now,
                CreateBy = "TEST"
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