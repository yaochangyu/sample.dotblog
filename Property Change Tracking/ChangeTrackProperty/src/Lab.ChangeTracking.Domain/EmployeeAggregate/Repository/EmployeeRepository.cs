using Lab.ChangeTracking.Infrastructure.DB.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.ChangeTracking.Domain;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbContextFactory<EmployeeDbContext> _memberContextFactory;

    public EmployeeRepository(IDbContextFactory<EmployeeDbContext> memberContextFactory)
    {
        this._memberContextFactory = memberContextFactory;
    }

    public Infrastructure.DB.EntityModel.Employee To(EmployeeEntity srcEmployee)
    {
        return new Infrastructure.DB.EntityModel.Employee
        {
            // Id = srcEmployee.Id,
            // Name = srcEmployee.Name,
            // Age = srcEmployee.Age,
            // Remark = srcEmployee.Remark,
            // CreateAt = srcEmployee.CreatedAt

            // CreateBy = srcEmployee.CreatedBy,
            // Identity = this.To(srcEmployee.Identity)
        };
    }

    public Identity To(IdentityEntity srcIdentity)
    {
        return new Identity
        {
            Password = srcIdentity.Password,
            Remark = srcIdentity.Remark,
            CreateAt = srcIdentity.CreateAt,
            CreateBy = srcIdentity.CreateBy
        };
    }

    public async Task<int> SaveChangeAsync(EmployeeEntity srcEmployee,
                                           CancellationToken cancel = default)
    {
        throw new Exception();
    }
}