using Lab.StepDependencyInjection.Test.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TechTalk.SpecFlow;

namespace Lab.StepDependencyInjection.Test.Steps;

[Binding]
public class GlobalSteps
{
    internal static ServiceProvider ServiceProvider;
    
    [BeforeTestRun]
    public static void BeforeTestRun()
    {
        var service = InstanceManager.InitializeServices();
        ServiceProvider = service.BuildServiceProvider();
        var factory = ServiceProvider.GetService<IDbContextFactory<EmployeeDbContext>>();
        using var dbContext = factory.CreateDbContext();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }
    
    [AfterTestRun]
    public static void AfterTestRun()
    {
        var factory = ServiceProvider.GetService<IDbContextFactory<EmployeeDbContext>>();
        using var dbContext = factory.CreateDbContext();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }
}