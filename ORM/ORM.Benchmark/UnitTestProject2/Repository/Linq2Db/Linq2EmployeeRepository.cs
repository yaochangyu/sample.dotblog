using System.Collections.Generic;
using System.Linq;
using UnitTestProject2.Repository.Linq2Db.EntityModel;
using UnitTestProject2.ViewModel;

namespace UnitTestProject2.Repository.Linq2Db
{
    public class Linq2EmployeeRepository : IEmployeeRepository
    {
        public Linq2EmployeeRepository(string connectionName)
        {
            this.ConnectionName = connectionName;
        }

        public string ConnectionName { get; set; }

        public IEnumerable<EmployeeViewModel> GetAllEmployees(out int count)
        {
            IEnumerable<EmployeeViewModel> results = null;
            using (var db = new LabEmployeeDB(this.ConnectionName))
            {
                var selector = db.Identities
                                 .Select(p => new EmployeeViewModel
                                 {
                                     Id = p.Employee.Id,
                                     Name = p.Employee.Name,
                                     Age = p.Employee.Age,
                                     SequenceId = p.Employee.SequenceId,

                                     Account = p.Account,
                                     Password = p.Password
                                 });
                //var selector = db.Employees
                //                 .Select(p => new EmployeeViewModel
                //                 {
                //                     Id = p.Id,
                //                     Name = p.Name,
                //                     Age = p.Age,
                //                     SequenceId = p.SequenceId,

                //                     Account = p.DboIdentitydboEmployeeId.Account,
                //                     Password = p.DboIdentitydboEmployeeId.Password
                //                 });

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