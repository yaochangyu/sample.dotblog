using Lab.ChangeTracking.Abstract;
using Lab.ChangeTracking.Infrastructure.DB.EntityModel;

namespace Lab.ChangeTracking.Domain;

static class EmployeeRepositoryExtensions
{
    public static Employee ToDataEntity(this IEmployeeEntity srcEmployee)
    {
        return new Employee
        {
            Id = srcEmployee.Id,
            Name = srcEmployee.Name,
            Age = srcEmployee.Age,
            Remark = srcEmployee.Remark,
            CreatedAt = srcEmployee.CreatedAt,
            CreatedBy = srcEmployee.CreatedBy,
            // Identity = this.To(srcEmployee.Identity)
        };
    }
 
}