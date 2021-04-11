using System;
using System.Threading;
using System.Threading.Tasks;
using Lab.DAL.DomainModel.Employee;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
                    this._dbContextFactory = DefaultDbContextFactory.GetInstance<IDbContextFactory<EmployeeContext>>();
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
            using var dbContext = this.DbContextFactory.CreateDbContext();
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