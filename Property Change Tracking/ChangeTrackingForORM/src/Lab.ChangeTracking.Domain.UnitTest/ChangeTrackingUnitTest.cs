using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ChangeTracking;
using Lab.ChangeTracking.Domain.Entity;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Lab.MultiTestCase.UnitTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ChangeTracking.Domain.UnitTest;

[TestClass]
public class ChangeTrackingUnitTest
{
    private readonly IEmployeeAggregate _employeeAggregate = TestAssistants.EmployeeAggregate;

    private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory =
        TestAssistants.EmployeeDbContextFactory;

    [TestMethod]
    public void 原本用法()
    {
        var employeeAggregate = new EmployeeAggregate(new EmployeeEntity()
        {
            Id = Guid.NewGuid(),
            Age = 18,
            Name = "Yao",
            Remark = "TEST"
        });
         employeeAggregate.SetName("小章");
         employeeAggregate.SubmitChange(DateTimeOffset.Now, "Sys1");
         
    }

    private static Employee Insert()
    {
        Console.WriteLine("新增資料");
        using var dbContext = TestAssistants.EmployeeDbContextFactory.CreateDbContext();
        var toDB = new Employee
        {
            Id = Guid.NewGuid(),
            Age = 18,
            Name = "yao",
            CreatedAt = DateTimeOffset.Now,
            CreatedBy = "TEST",
            Identity = new Identity
            {
                Account = "yao",
                Password = "123456",
                CreatedAt = DateTimeOffset.Now,
                CreatedBy = "TEST",
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