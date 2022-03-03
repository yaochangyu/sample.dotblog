using Lab.ChangeTracking.Abstract;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbContextFactory<EmployeeDbContext> _employeeContextFactory;

    public EmployeeRepository(IDbContextFactory<EmployeeDbContext> employeeContextFactory)
    {
        this._employeeContextFactory = employeeContextFactory;
    }

    public Employee To(EmployeeEntity srcEmployee)
    {
        return new Employee
        {
            Id = srcEmployee.Id,
            Name = srcEmployee.Name,
            Age = srcEmployee.Age,
            Remark = srcEmployee.Remark,
            CreatedAt = srcEmployee.CreatedAt,
            CreatedBy = srcEmployee.CreatedBy,
            Identity = this.To(srcEmployee.Identity)
        };
    }

    public Identity To(IdentityEntity srcIdentity)
    {
        return new Identity
        {
            Password = srcIdentity.Password,
            Remark = srcIdentity.Remark,
            CreatedAt = srcIdentity.CreateAt,
            CreatedBy = srcIdentity.CreateBy,
        };
    }

    public async Task<int> SaveChangeAsync(IEmployeeAggregate<IEmployeeEntity> srcEmployee,
                                           CancellationToken cancel = default)
    {
        // var employeeEntity = srcEmployee.GetInstance();
        var employee = new Employee();
        foreach (var change in srcEmployee.GetChangeActions())
        {
            change(employee);
        }

        await using var dbContext = await this._employeeContextFactory.CreateDbContextAsync(cancel);
        return 1;
    }

    public async Task<int> SaveChangeAsync(EmployeeAggregate srcEmployee, CancellationToken cancel = default)
    {
        await using var dbContext = await this._employeeContextFactory.CreateDbContextAsync(cancel);

        if (srcEmployee.State != EntityState.Submitted)
        {
            throw new Exception($"尚未 {nameof(EntityState.Submitted)}，不能存檔");
        }

        var employeeFromDb = await dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == srcEmployee.Id, cancel);
        if (employeeFromDb == null)
        {
            var toDb = new Employee();
            foreach (var changeAction in srcEmployee.GetChangeActions())
            {
                changeAction(toDb);
            }

            dbContext.Add(toDb);
        }
        else
        {
            foreach (var changeAction in srcEmployee.GetChangeActions())
            {
                changeAction(employeeFromDb);
            }
        }

        return await dbContext.SaveChangesAsync(cancel);
    }

    public Task<int> SaveChangeAsync(IEmployeeEntity employee, CancellationToken cancel = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IEmployeeEntity> GetAsync(Guid id, CancellationToken cancel = default)
    {
        await using var dbContext = await this._employeeContextFactory.CreateDbContextAsync(cancel);
        return await dbContext.Employees
            .Where(p => p.Id == id)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancel);
    }
}