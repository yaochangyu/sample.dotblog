using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Lab.ChangeTracking.Abstract;
using Lab.ChangeTracking.Domain.EmployeeAggregate;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Lab.MultiTestCase.UnitTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Lab.ChangeTracking.Domain.UnitTest;

[TestClass]
public class ChangeTrackingUnitTest
{
    private readonly IEmployeeAggregate<IEmployeeEntity> _employeeAggregate = TestAssistants.EmployeeAggregate;

    private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory =
        TestAssistants.EmployeeDbContextFactory;

    [TestMethod]
    public async Task 新增()
    {
        // arrange
        var employeeRepository = TestAssistants.EmployeeRepository;
        var systemClock = Substitute.For<ISystemClock>();
        systemClock.Now.Returns(DateTimeOffset.Parse("2021-01-01"));
        var uuIdProvider = Substitute.For<IUUIdProvider>();
        uuIdProvider.GenerateId().Returns(TestAssistants.Parse("1"));
        var accessContext = Substitute.For<IAccessContext>();
        accessContext.AccessNow.Returns(DateTimeOffset.Parse("2021-01-02"));
        accessContext.AccessUserId.Returns("System User");

        // act
        var employeeAggregate =
            new EmployeeAggregate.EmployeeAggregate(employeeRepository, uuIdProvider, systemClock, accessContext);
        employeeAggregate.Initial("yao", 18, "Test User");
        employeeAggregate.SubmitChange();
        var result = await employeeAggregate.SaveChangeAsync();

        // assert
        await using var dbContext = await this._employeeDbContextFactory.CreateDbContextAsync();
        var actual = dbContext.Employees.AsTracking().FirstOrDefault();
        Assert.AreEqual("yao", actual.Name);
        Assert.AreEqual(18, actual.Age);
        Assert.AreEqual(1, actual.Version);
        Assert.AreEqual("Test User", actual.Remark);
    }

    [TestMethod]
    public async Task 編輯()
    {
        // arrange
        InsertTestData();
        var employeeRepository = TestAssistants.EmployeeRepository;
        var systemClock = Substitute.For<ISystemClock>();
        systemClock.Now.Returns(DateTimeOffset.Parse("2021-01-02"));
        var uuIdProvider = Substitute.For<IUUIdProvider>();
        uuIdProvider.GenerateId().Returns(TestAssistants.Parse("1"));
        var accessContext = Substitute.For<IAccessContext>();
        accessContext.AccessNow.Returns(DateTimeOffset.Parse("2021-01-02"));

        // act
        var employeeAggregate =
            new EmployeeAggregate.EmployeeAggregate(employeeRepository, uuIdProvider, systemClock, accessContext);
        await employeeAggregate.GetAsync(TestAssistants.Parse("1"));
        employeeAggregate.SetName("小章").SetAge(20);
        employeeAggregate.SubmitChange();
        var count = await employeeAggregate.SaveChangeAsync();

        // assert
        await using var dbContext = await this._employeeDbContextFactory.CreateDbContextAsync();
        var actual = dbContext.Employees.AsTracking().FirstOrDefault();
        Assert.AreEqual("小章", actual.Name);
        Assert.AreEqual(20, actual.Age);
        Assert.AreEqual(2, actual.Version);
    }
    [TestMethod]
    public async Task 編輯1()
    {
        // arrange
        InsertTestData();
        await using var dbContext = await TestAssistants.EmployeeDbContextFactory.CreateDbContextAsync();

        var employee = dbContext.Employees
            .Include(p => p.Profiles)
            .AsTracking()
            // .Load()
            .FirstOrDefault()
            ;
        var now = DateTimeOffset.Now;
        var accessUserId = "TEST USER";
        var newProfile = new Profile
        {
            Id = Guid.NewGuid(),
            Employee_Id = employee.Id,
            CreatedAt = now,
            CreatedBy = accessUserId,
            UpdatedAt = now,
            UpdatedBy = accessUserId,
            FirstName = "first name",
            LastName = "last name",
        };
        employee.Profiles.Add(newProfile);
        // dbContext.Profiles.Add(newProfile);
        await dbContext.SaveChangesAsync();
    }   

    private static Employee InsertTestData()
    {
        Console.WriteLine("新增資料");
        using var dbContext = TestAssistants.EmployeeDbContextFactory.CreateDbContext();
        var employeeId = TestAssistants.Parse("1");
        var now = DateTimeOffset.Now;
        var accessUserId = "TEST USER";
        var toDB = new Employee
        {
            Id = employeeId,
            Age = 18,
            Name = "yao",
            CreatedAt = now,
            CreatedBy = accessUserId,
            Identity = new()
            {
                Employee_Id = employeeId, 
                Account = "yao",
                Password = "123456",
                CreatedAt = now,
                CreatedBy = accessUserId,
                UpdatedAt = now,
                UpdatedBy = accessUserId,
            },
            Version = 1,
            Profiles = new List<Profile>()
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Employee_Id = employeeId,
                    CreatedAt = now,
                    CreatedBy = accessUserId,
                    UpdatedAt = now,
                    UpdatedBy = accessUserId,
                    FirstName = "yao",
                    LastName = "yu",
                }
            },
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