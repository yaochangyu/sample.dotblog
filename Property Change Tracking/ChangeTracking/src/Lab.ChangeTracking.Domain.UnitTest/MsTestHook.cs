using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.MultiTestCase.UnitTest;

[TestClass]
public class MsTestHook
{
    [AssemblyCleanup]
    public static void Cleanup()
    {
        TestAssistants.SetTestEnvironmentVariable();
        var db = TestAssistants.EmployeeDbContextFactory.CreateDbContext();
        if (db.Database.CanConnect())
        {
            db.Database.EnsureDeleted();
        }
    }

    [AssemblyInitialize]
    public static void Setup(TestContext context)
    {
        TestAssistants.SetTestEnvironmentVariable();
        var db = TestAssistants.EmployeeDbContextFactory.CreateDbContext();
        if (db.Database.CanConnect())
        {
            db.Database.EnsureDeleted();
        }

        db.Database.EnsureCreated();
    }
}