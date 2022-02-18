using System;
using System.Collections.Generic;
using System.ComponentModel;
using ChangeTracking;
using Lab.ChangeTracking.Domain.EmployeeAggregate;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Lab.MultiTestCase.UnitTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ChangeTracking.Domain.UnitTest;

[TestClass]
public class ChangeTrackingUnitTest
{
    private IEmployeeAggregate _employeeAggregate = TestAssistants.EmployeeAggregate;

    public ChangeTrackingUnitTest()
    {
    }

    [TestMethod]
    public void 原本用法()
    {
        var source = new EmployeeEntity()
        {
            Id = Guid.NewGuid(),
            Name = "yao",
            Age = 12,
        };
        source.Age = 18;
        Assert.AreEqual(18, source.Age);
    }

    [TestMethod]
    public void 追蹤()
    {
        var source = new EmployeeEntity()
        {
            Id = Guid.NewGuid(),
            Name = "yao",
            Age = 12,
        };
        var tracked = source.AsTrackable();
        tracked.Age = 18;

        // source.Age = 18;
        var trackable = tracked.CastToIChangeTrackable();

        Assert.AreEqual(18, source.Age);
    }

    [TestMethod]
    public void 追蹤後異動()
    {
        var toDB = Insert();
        var source = new EmployeeEntity()
        {
            Id = toDB.Id,
            Name = "yao",
            Age = 12,
            Identity = new IdentityEntity(){},
            Profiles = new Dictionary<string, string>()
            
        };
        var employeeEntity = this._employeeAggregate.ModifyFlowAsync(source).Result;
    }

    private static Employee Insert()
    {
        using var dbContext = TestAssistants.EmployeeDbContextFactory.CreateDbContext();
        var toDB = new Employee()
        {
            Id = Guid.NewGuid(),
            Age = 18,
            Name = "yao",
            CreateAt = DateTimeOffset.Now,
            CreateBy = "TEST",
            Identity = new Identity()
            {
                Account = "yao",
                Password = "123456",
                CreateAt = DateTimeOffset.Now,
                CreateBy = "TEST",
            }
        };
        dbContext.Employees.Add(toDB);
        dbContext.SaveChanges();
        return toDB;
    }

  
}