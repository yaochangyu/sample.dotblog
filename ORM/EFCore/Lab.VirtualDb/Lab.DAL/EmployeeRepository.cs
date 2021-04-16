using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lab.DAL.DomainModel.Employee;
using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.DAL
{
    public interface IEmployeeRepository
    {
        Task<int> InsertLogAsync(InsertOrderRequest request,
                                 string             accessId,
                                 CancellationToken  cancel = default);

        Task<int> NewAsync(NewRequest        request,
                           string            accessId,
                           CancellationToken cancel = default);
    }

    public class EmployeeRepository : IEmployeeRepository
    {
        internal IDbContextFactory<EmployeeDbContext> DbContextFactory
        {
            get
            {
                if (this._dbContextFactory == null)
                {
                    return DefaultDbContextManager.GetInstance<IDbContextFactory<EmployeeDbContext>>();
                }

                return this._dbContextFactory;
            }
            set => this._dbContextFactory = value;
        }

        internal EmployeeDbContext EmployeeDbContext
        {
            get
            {
                if (this._employeeDbContext == null)
                {
                    return this.DbContextFactory.CreateDbContext();
                }

                return this._employeeDbContext;
            }
            set => this._employeeDbContext = value;
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

        private IDbContextFactory<EmployeeDbContext> _dbContextFactory;
        private EmployeeDbContext                    _employeeDbContext;
        private DateTime?                            _now;

        public async Task<int> InsertLogAsync(InsertOrderRequest request,
                                              string             accessId,
                                              CancellationToken  cancel = default)
        {
            await using var dbContext = this.EmployeeDbContext;

            var toDbOrderHistory = new OrderHistory
            {
                Employee_Id  = request.Employee_Id,
                Product_Id   = request.Product_Id,
                Product_Name = request.Product_Name,
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
            await using var dbContext = this.EmployeeDbContext;

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

        public async Task<IList<FilterResponse>> GetAllAsync(CancellationToken cancel)
        {
            await using var db = this.EmployeeDbContext;
            return await db.Employees
                           .Include(p => p.Identity)
                           .Select(p => new FilterResponse()
                           {
                               Account  = p.Identity.Account,
                               Age      = p.Age,
                               Name     = p.Name,
                               Password = p.Identity.Password,
                               Remark   = p.Remark
                           })
                           .AsNoTracking()
                           .ToListAsync(cancel);
        }
    }
}