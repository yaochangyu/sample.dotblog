using System;
using System.Threading;
using System.Threading.Tasks;
using Lab.DAL.DomainModel.Employee;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.DAL
{
    public class EmployeeRepository
    {
        private readonly IDbContextFactory<EmployeeContext> _factory;

        // public EmployeeRepository(IDbContextFactory<EmployeeContext> factory)
        // {
        //     this._factory = factory;
        // }
        public EmployeeRepository(EmployeeContext factory)
        {
        } 
        public async Task<int> InsertAsync(InsertRequest request, 
                                           string accessId,
                                           CancellationToken cancel = default)
        {
            using var dbContext = this._factory.CreateDbContext();
            var       id        = Guid.NewGuid();
            var toDb = new Employee
            {
            Id   = id,
                Name = "yao",
                Age  = 18,
            };
            await dbContext.Employees.AddAsync(toDb, cancel);
            return await dbContext.SaveChangesAsync(cancel);
        }
    }
}