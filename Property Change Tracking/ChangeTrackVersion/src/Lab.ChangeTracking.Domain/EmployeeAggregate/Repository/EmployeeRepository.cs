using ChangeTracking;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain;

public class EmployeeRepository
{
    private readonly IDbContextFactory<EmployeeDbContext> _employeeDbContextFactory;

    public EmployeeRepository(IDbContextFactory<EmployeeDbContext> memberContextFactory)
    {
        this._employeeDbContextFactory = memberContextFactory;
    }

    public async Task<int> AddAsync(EmployeeEntity source,
                                    CancellationToken cancel = default)
    {
        var dbContext = await this._employeeDbContextFactory.CreateDbContextAsync(cancel);
        var srcEmployee = source.CastToIChangeTrackable();

        return await dbContext.SaveChangesAsync(cancel);
    }

    public async Task<int> SaveChangesAsync(EmployeeEntity source,
                                            CancellationToken cancel = default)
    {
        var dbContext = await this._employeeDbContextFactory.CreateDbContextAsync(cancel);
        var employeeTrackable = source.CastToIChangeTrackable();

        return await dbContext.SaveChangesAsync(cancel);
    }

    public async Task<int> InsertEmployeeAsync(EmployeeEntity srcEmployee, CancellationToken cancel = default)
    {
        var destEmployee = srcEmployee.To();
        var dbContext = await this._employeeDbContextFactory.CreateDbContextAsync(cancel);
        await dbContext.AddAsync(destEmployee, cancel);
        return await dbContext.SaveChangesAsync(cancel);
    }
}