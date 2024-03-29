using Lab.StepDependencyInjection.WebAPI.EntityModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab.StepDependencyInjection.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class DefaultController : ControllerBase
{
    private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory;

    public DefaultController(IDbContextFactory<EmployeeDbContext> employeeDbContextFactory)
    {
        this._employeeDbContextFactory = employeeDbContextFactory;
    }

    [HttpGet()]
    public async Task<ActionResult> Get()
    {
        await using var dbContext = await this._employeeDbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureCreatedAsync();
        var data = await dbContext.Employees.Where(p => p.Age == 10).AsNoTracking().ToListAsync();
        return this.Ok(data);
    }
}