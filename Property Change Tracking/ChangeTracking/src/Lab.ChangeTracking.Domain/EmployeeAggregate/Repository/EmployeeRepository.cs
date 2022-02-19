using ChangeTracking;
using EFCore.BulkExtensions;
using Lab.ChangeTracking.Domain.EmployeeAggregate.Entity;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain.EmployeeAggregate.Repository;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbContextFactory<EmployeeDbContext> _memberContextFactory;

    public EmployeeRepository(IDbContextFactory<EmployeeDbContext> memberContextFactory)
    {
        this._memberContextFactory = memberContextFactory;
    }

    public Employee To(EmployeeEntity srcEmployee)
    {
        return new Employee
        {
            Id = srcEmployee.Id,
            Name = srcEmployee.Name,
            Age = srcEmployee.Age,
            Remark = srcEmployee.Remark,
            CreateAt = srcEmployee.CreateAt,
            CreateBy = srcEmployee.CreateBy,
            Identity = this.To(srcEmployee.Identity)
        };
    }

    public Identity To(IdentityEntity srcIdentity)
    {
        return new Identity
        {
            Password = srcIdentity.Password,
            Remark = srcIdentity.Remark,
            CreateAt = srcIdentity.CreateAt,
            CreateBy = srcIdentity.CreateBy,
        };
    }

    public async Task<int> SaveChangeAsync(EmployeeEntity srcEmployee,
                                           CancellationToken cancel = default)
    {
        var employeeTrackable = srcEmployee.CastToIChangeTrackable();
        var identityTrackable = srcEmployee.Identity.CastToIChangeTrackable();
        var memberChangeProperties = employeeTrackable.ChangedProperties.ToList();
        var identityChangeProperties = identityTrackable.ChangedProperties.ToList();

        await using var dbContext = await this._memberContextFactory.CreateDbContextAsync(cancel);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancel);

        try
        {
            var destEmployee = this.To(srcEmployee);
            var memberChangeCount = await dbContext.Employees
                                                   .Where(a => a.Id == srcEmployee.Id)
                                                   .BatchUpdateAsync(destEmployee,
                                                                     memberChangeProperties, cancel);
            var identityChangeCount = await dbContext.Identities
                                                     .Where(a => a.Employee_Id == srcEmployee.Id)
                                                     .BatchUpdateAsync(destEmployee.Identity,
                                                                       identityChangeProperties,
                                                                       cancel);

            await transaction.CommitAsync(cancel);
            return memberChangeCount + identityChangeCount;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(cancel);
            throw new Exception("存檔失敗");
        }

        return 0;
    }

    public async Task<int> SaveChange1Async(EmployeeEntity srcEmployee,
                                            CancellationToken cancel = default)
    {
        var employeeTrackable = srcEmployee.CastToIChangeTrackable();
        var identityTrackable = srcEmployee.Identity.CastToIChangeTrackable();
        var profileTrackable = srcEmployee.Profiles.CastToIChangeTrackableCollection();

        var memberChangeProperties = employeeTrackable.ChangedProperties.ToList();
        var identityChangeProperties = identityTrackable.ChangedProperties.ToList();
        var changedItems = profileTrackable.ChangedItems;
        var addedItems = profileTrackable.AddedItems;
        var unchangedItems = profileTrackable.UnchangedItems;
        var deletedItems = profileTrackable.DeletedItems;

        await using var dbContext = await this._memberContextFactory.CreateDbContextAsync(cancel);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancel);

        try
        {
            var destEmployee = this.To(srcEmployee);
            var memberChangeCount = await dbContext.Employees
                                                   .Where(a => a.Id == srcEmployee.Id)
                                                   .BatchUpdateAsync(destEmployee,
                                                                     memberChangeProperties, cancel);
            var identityChangeCount = await dbContext.Identities
                                                     .Where(a => a.Employee_Id == srcEmployee.Id)
                                                     .BatchUpdateAsync(destEmployee.Identity,
                                                                       identityChangeProperties,
                                                                       cancel);

            await transaction.CommitAsync(cancel);
            return memberChangeCount + identityChangeCount;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync(cancel);
            throw new Exception("存檔失敗");
        }

        return 0;
    }
}