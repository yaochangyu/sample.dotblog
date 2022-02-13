using System;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ChangeTracking.Domain.UnitTest;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void AddRanges()
    {
        var source = new Employee()
        {
            Id = Guid.NewGuid(),
            Age = 18,
            Name = "yao"
        };

        source.Age = 20;
        Assert.AreEqual(18,source.Age);
    }

}