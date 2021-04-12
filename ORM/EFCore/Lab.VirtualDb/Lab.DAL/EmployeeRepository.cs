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
        internal IDbContextFactory<EmployeeContext> DbContextFactory
        {
            get
            {
                if (this._dbContextFactory == null)
                {
                    this._dbContextFactory = DefaultDbContextManager.GetInstance<IDbContextFactory<EmployeeContext>>();
                }

                return this._dbContextFactory;
            }
            set => this._dbContextFactory = value;
        }

        private IDbContextFactory<EmployeeContext> _dbContextFactory;

        public async Task<int> InsertAsync(InsertRequest     request,
                                           string            accessId,
                                           CancellationToken cancel = default)
        {
            await using var dbContext = this.DbContextFactory.CreateDbContext();

            var id = Guid.NewGuid();
            var toDbEmployee = new Employee
            {
                Id     = id,
                Name   = request.Name,
                Age    = request.Age,
                Remark = request.Remark,
            };
            var toDbIdentity = new Identity
            {
                Account  = request.Account,
                Password = request.Password,
                Remark   = request.Remark,
                Employee = toDbEmployee
            };
            toDbEmployee.Identity = toDbIdentity;
            await dbContext.Employees.AddAsync(toDbEmployee, cancel);
            try
            {
                return await dbContext.SaveChangesAsync(cancel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}