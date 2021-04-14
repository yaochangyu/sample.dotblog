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

        internal DateTime Now
        {
            get
            {
                if (this._now == null)
                {
                    return DefaultDbContextManager.Now;
                }

                return this._now.Value;
            }
            set => this._now = value;
        }

        private IDbContextFactory<EmployeeContext> _dbContextFactory;
        private DateTime?                          _now;

        public async Task<int> InsertLogAsync(InsertOrderRequest request,
                                              string             accessId,
                                              CancellationToken  cancel = default)
        {
            await using var dbContext = this.DbContextFactory.CreateDbContext();

            var toDbOrderHistory = new OrderHistory
            {
                Employee_Id  = request.Employee_Id,
                Product_Id   = request.Product_Id,
                Product_Name = request.Product_Id,
                CreateAt     = this.Now,
                CreateBy     = accessId,
                Remark       = request.Remark,
            };

            await dbContext.OrderHistories.AddAsync(toDbOrderHistory, cancel);
            return await dbContext.SaveChangesAsync(cancel);
        }

        public async Task<int> NewAsync(NewRequest        request,
                                        string            accessId,
                                        CancellationToken cancel = default)
        {
            await using var dbContext = this.DbContextFactory.CreateDbContext();

            var id = Guid.NewGuid();
            var employeeToDb = new Employee
            {
                Id       = id,
                Name     = request.Name,
                Age      = request.Age,
                Remark   = request.Remark,
                CreateAt = this.Now,
                CreateBy = accessId
            };
            
            var identityToDb = new Identity
            {
                Account  = request.Account,
                Password = request.Password,
                Remark   = request.Remark,
                Employee = employeeToDb,
                CreateAt = this.Now,
                CreateBy = accessId
            };
            
            employeeToDb.Identity = identityToDb;
            await dbContext.Employees.AddAsync(employeeToDb, cancel);
            return await dbContext.SaveChangesAsync(cancel);
        }
    }
}