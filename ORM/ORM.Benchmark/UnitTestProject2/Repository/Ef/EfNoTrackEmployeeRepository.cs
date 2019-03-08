using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using UnitTestProject2.Repository.Ef.EntityModel;
using UnitTestProject2.ViewModel;

namespace UnitTestProject2.Repository.Ef
{
    public class EfNoTrackEmployeeRepository : IEmployeeRepository
    {
        public EfNoTrackEmployeeRepository(string connectionName)
        {
            this.ConnectionName = connectionName;
        }

        public string ConnectionName { get; set; }

        public IEnumerable<EmployeeViewModel> GetAllEmployees(out int count)
        {
            IEnumerable<EmployeeViewModel> results = null;
            using (var dbContext = new LabDbContext(this.ConnectionName))
            {
                dbContext.Configuration.LazyLoadingEnabled = false;
                dbContext.Configuration.ProxyCreationEnabled = false;
                dbContext.Configuration.AutoDetectChangesEnabled = false;

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
                results = selector.AsNoTracking().ToList();
            }

            return results;
        }
    }
}