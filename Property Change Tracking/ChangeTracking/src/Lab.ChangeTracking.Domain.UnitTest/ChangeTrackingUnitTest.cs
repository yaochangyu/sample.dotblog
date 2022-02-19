using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using ChangeTracking;
using Lab.ChangeTracking.Domain.EmployeeAggregate;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;
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
        var source = new EmployeeEntity
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
        var source = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            Name = "yao",
            Age = 12,
            Identity = new IdentityEntity { Account = "G1234" },
        };
        var trackable = source.AsTrackable();
        trackable.Name = "小章";
        var employTrackable = trackable.CastToIChangeTrackable();

        var employeeChangedProperties = employTrackable.ChangedProperties;

        Console.WriteLine($"{nameof(this.追蹤)}:追蹤欄位");
        Console.WriteLine(ToJson(employeeChangedProperties));
    }

    [TestMethod]
    public void 追蹤集合()
    {
        var source = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            Name = "yao",
            Age = 12,
            Identity = new IdentityEntity { Account = "G1234" },
            Profiles = new List<ProfileEntity>
            {
                new() { FirstName = "第一筆" },
                new() { FirstName = "將被刪掉" },
            }
        };
        var trackable = source.AsTrackable();
        trackable.Profiles[0].FirstName = "變更";
        trackable.Profiles.Add(new ProfileEntity { FirstName = "新增" });
        trackable.Profiles.RemoveAt(1);

        var profileTrackable = trackable.Profiles.CastToIChangeTrackableCollection();

        var unchangedItems = profileTrackable.UnchangedItems;
        var addedItems = profileTrackable.AddedItems;
        var changedItems = profileTrackable.ChangedItems;
        var deleteItems = profileTrackable.DeletedItems;

        Console.WriteLine($"{nameof(this.追蹤集合)}:追蹤集合");
        Console.WriteLine($"UnchangedItems:{ToJson(unchangedItems)}");
        Console.WriteLine($"AddItem:{ToJson(addedItems)}");
        Console.WriteLine($"ChangedItems:{ToJson(changedItems)}");
        Console.WriteLine($"DeleteItems:{ToJson(deleteItems)}");
        Console.WriteLine($"{nameof(this.追蹤集合)}:追蹤變更屬性");
        var changeTrackable = trackable.Profiles[0].CastToIChangeTrackable();
        Console.WriteLine($"變更欄位:{ToJson(changeTrackable.ChangedProperties)}");
    }

    [TestMethod]
    public void 追蹤複雜型別()
    {
        var source = new EmployeeEntity
        {
            Id = Guid.NewGuid(),
            Name = "yao",
            Age = 12,
            Identity = new IdentityEntity { Account = "G1234" },
        };
        var trackable = source.AsTrackable();
        trackable.Name = "小章";
        trackable.Identity.Account = "yao";
        var employTrackable = trackable.CastToIChangeTrackable();
        var identityTrackable = trackable.Identity.CastToIChangeTrackable();

        var employeeChangedProperties = employTrackable.ChangedProperties;
        var identityChangedProperties = identityTrackable.ChangedProperties;

        Console.WriteLine($"{nameof(this.追蹤複雜型別)}:追蹤欄位");
        Console.WriteLine(ToJson(employeeChangedProperties));
        Console.WriteLine(ToJson(identityChangedProperties));
    }

    [TestMethod]
    public void 異動追蹤後存檔()
    {
        var toDB = Insert();
        var source = new EmployeeEntity
        {
            Id = toDB.Id,
            Name = "yao",
            Age = 12,
            Identity = new IdentityEntity(),
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
                CreateBy = "TEST",
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