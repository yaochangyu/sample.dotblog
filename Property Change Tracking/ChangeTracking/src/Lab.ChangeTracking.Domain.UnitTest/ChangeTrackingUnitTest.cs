using System;
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
    public void Repository_追蹤()
    {
        var source = new EmployeeEntity()
        {
            Id = Guid.NewGuid(),
            Name = "yao",
            Age = 12,
        };
        var employeeEntity = this._employeeAggregate.ModifyAsync(source).Result;
    }
}