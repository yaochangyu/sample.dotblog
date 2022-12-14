using Lab.StepDependencyInjection.Test.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SolidToken.SpecFlow.DependencyInjection;
using TechTalk.SpecFlow;

namespace Lab.StepDependencyInjection.Test.Steps;

[Binding]
[Scope(Feature = "Demo0")]
public class Demo0Steps
{
    private readonly FileProvider _fileProvider;

    private ScenarioContext ScenarioContext { get; }

    private IDbContextFactory<EmployeeDbContext> _dbContextFactory;

    public Demo0Steps(ScenarioContext scenarioContext,
        FileProvider fileProvider,
        IDbContextFactory<EmployeeDbContext> dbContextFactory)
    {
        this.ScenarioContext = scenarioContext;
        this._fileProvider = fileProvider;
        this._dbContextFactory = dbContextFactory;
    }

    [ScenarioDependencies]
    public static IServiceCollection CreateServices()
    {
        return InstanceManager.InitializeServices();
    }

    [When(@"寫入資料表")]
    public void When寫入資料表()
    {
        // var dbContextFactory = GlobalSteps.ServiceProvider.GetService<IDbContextFactory<EmployeeDbContext>>();
        var dbContextFactory = this._dbContextFactory;
        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Add(new Employee
        {
            Id = Guid.NewGuid(),
            Name = "yao",
            Age = 19,
            CreateAt = DateTime.UtcNow,
            CreateBy = "yao"
        });
        dbContext.SaveChanges();
    }

    [When(@"讀取資料表")]
    public void When讀取資料表()
    {
        // var dbContextFactory = GlobalSteps.ServiceProvider.GetService<IDbContextFactory<EmployeeDbContext>>();
        var dbContextFactory = this._dbContextFactory;
        using var dbContext = dbContextFactory.CreateDbContext();
        var employees = dbContext.Employees.AsNoTracking().ToList();
    }
}