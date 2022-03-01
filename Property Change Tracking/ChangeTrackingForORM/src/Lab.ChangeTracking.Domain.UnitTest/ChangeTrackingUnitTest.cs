using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Lab.ChangeTracking.Abstract;
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

        // act
        var employeeAggregate = new EmployeeAggregate(employeeRepository, uuIdProvider, systemClock);
        employeeAggregate.CreateInstance("yao", 18, "Test User");
        employeeAggregate.SubmitChange(systemClock.Now, "Sys1");
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

        // act
        var employeeAggregate = new EmployeeAggregate(employeeRepository, uuIdProvider, systemClock);
        await employeeAggregate.GetAsync(TestAssistants.Parse("1"));
        employeeAggregate.SetName("小章");
        employeeAggregate.SetAge(20);
        employeeAggregate.SubmitChange(systemClock.Now, "Sys1");
        var count = await employeeAggregate.SaveChangeAsync();

        // assert
        await using var dbContext = await this._employeeDbContextFactory.CreateDbContextAsync();
        var actual = dbContext.Employees.AsTracking().FirstOrDefault();
        Assert.AreEqual("小章", actual.Name);
        Assert.AreEqual(20, actual.Age);
        Assert.AreEqual(2, actual.Version);
    }

    private static Employee InsertTestData()
    {
        Console.WriteLine("新增資料");
        using var dbContext = TestAssistants.EmployeeDbContextFactory.CreateDbContext();
        var toDB = new Employee
        {
            Id = TestAssistants.Parse("1"),
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
            },
            Version = 1
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