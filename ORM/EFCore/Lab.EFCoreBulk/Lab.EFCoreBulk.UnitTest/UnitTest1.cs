using System;
using Lab.EFCoreBulk.EntityModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.EFCoreBulk.UnitTest;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
        var db = TestInstanceManager.EmployeeDbContextFactory.CreateDbContext();
        var id = Guid.NewGuid();
        db.Employees.Add(new Employee
        {
            Id = id,
            Age = 18,
            CreateAt = DateTimeOffset.UtcNow,
            Name = "yao",
            CreateBy = "Sys",
            // Identity = new Identity
            // {
            //     Account = "yao",
            //     CreateAt = DateTimeOffset.UtcNow,
            //     CreateBy = "Sys",
            //     Password = "123456",
            // },
        });
        db.SaveChanges();
    }
}