using System.Collections.Generic;
using System.Linq;
using UnitTestProject2.Repository.Ef.EntityModel;
using UnitTestProject2.ViewModel;

namespace UnitTestProject2.Repository.Ef
{
    public class EfEmployeeRepository : IEmployeeRepository
    {
        public EfEmployeeRepository(string connectionName)
        {
            this.ConnectionName = connectionName;
        }

        public string ConnectionName { get; set; }

        public object GetAll(out int count)
        {
            IEnumerable<Employee> results = null;
            using (var dbContext = new LabDbContext(this.ConnectionName))
            {
                var selector = dbContext.Employees.AsQueryable();

                count = selector.Count();
                if (count == 0)
                {
                    return results;
                }

                selector = selector.OrderBy(p => p.SequenceId);
                results = selector.ToList();
            }

            return results;
        }

        public IEnumerable<EmployeeViewModel> GetAllEmployees(out int count)
        {
            IEnumerable<EmployeeViewModel> results = null;
            using (var dbContext = new LabDbContext(this.ConnectionName))
            {
                dbContext.Configuration.LazyLoadingEnabled = true;
                dbContext.Configuration.ProxyCreationEnabled = true;
                dbContext.Configuration.AutoDetectChangesEnabled = true;
                var selector = dbContext.Employees
                                        .Select(p => new EmployeeViewModel
                                        {
                                            Id = p.Id,
                                            Name = p.Name,
                                            Age = p.Age,
                                            SequenceId = p.SequenceId
                                        });

                results = selector.ToList();
                count = results.Count();
            }

            return results;
        }

        public IEnumerable<EmployeeViewModel> GetAllEmployeesDetail(out int count)
        {
            IEnumerable<EmployeeViewModel> results = null;
            using (var dbContext = new LabDbContext(this.ConnectionName))
            {
                var selector = dbContext.Identities
                                        .Select(p => new EmployeeViewModel
                                        {
                                            Id = p.Employee.Id,
                                            Name = p.Employee.Name,
                                            Age = p.Employee.Age,
                                            SequenceId = p.Employee.SequenceId,

                                            Account = p.Account,
                                            Password = p.Password
                                        });

                count = selector.Count();
                if (count == 0)
                {
                    return results;
                }

                selector = selector.OrderBy(p => p.SequenceId);
                results = selector.ToList();
            }

            return results;
        }
    }
}