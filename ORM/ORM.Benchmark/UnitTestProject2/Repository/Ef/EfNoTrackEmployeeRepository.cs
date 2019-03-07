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
            var totalCount = 0;
            using (var dbContext = new LabDbContext(this.ConnectionName))
            {
                dbContext.Employees.Add(new Employee()
                {
                    Id = Guid.NewGuid(),
                    Name = "yao",
                    Age = 19,
                    Identity = new Identity()
                    {
                        Account = "yao",
                        Password = "1234"
                    }
                });
                dbContext.SaveChanges();
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

                selector.OrderBy(p => p.SequenceId);
                results = selector.AsNoTracking().ToList();
            }

            return results;
        }
    }
}