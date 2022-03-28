using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.ORM.DynamicField.UnitTest;

[TestClass]
public class GlobalSteps
{
    [AssemblyCleanup]
    public static void Cleanup()
    {
        // TestAssistant.SetTestEnvironmentVariable();
        // using var db = TestAssistant.EmployeeDbContextFactory.CreateDbContext();
        // db.Database.EnsureDeleted();
    }

    [AssemblyInitialize]
    public static void Setup(TestContext context)
    {
        TestAssistant.SetTestEnvironmentVariable();
        using var db = TestAssistant.EmployeeDbContextFactory.CreateDbContext();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}