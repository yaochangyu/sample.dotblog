using System.Collections.Generic;
using System.Linq;
using Dapper;
using UnitTestProject2.Repository.Ef.EntityModel;
using UnitTestProject2.ViewModel;

namespace UnitTestProject2.Repository.Dapper
{
    public class DapperEmployeeRepository : IEmployeeRepository
    {
        public DapperEmployeeRepository(string connectionName)
        {
            this.ConnectionName = connectionName;
        }

        public string ConnectionName { get; set; }

        public object GetAll(out int count)
        {
            IEnumerable<Employee> results = null;

            count = 0;
            var countText = @"
SELECT
	Count(*)
FROM
	[dbo].[Employee] [p]
";
            var selectText = @"
SELECT
	[p].[Id],
	[p].[Name],
	[p].[Age],
	[p].[SequenceId]
FROM
	[dbo].[Employee] [p]
ORDER BY
	[p].[SequenceId]
";

            using (var db = DbManager.CreateConnection(this.ConnectionName))
            {
                count = db.QueryFirst<int>(countText);
                if (count == 0)
                {
                    return results;
                }

                results = db.Query<Employee>(selectText);
            }

            return results;
        }

        public IEnumerable<EmployeeViewModel> GetAllEmployees(out int count)
        {
            IEnumerable<EmployeeViewModel> results = null;

            count = 0;

            using (var db = DbManager.CreateConnection(this.ConnectionName))
            {
                results = db.Query<EmployeeViewModel>(SqlEmployeeText.AllEmployee);
                count = results.Count();
            }

            return results;
        }

        public IEnumerable<EmployeeViewModel> GetAllEmployeesDetail(out int count)
        {
            IEnumerable<EmployeeViewModel> results = null;

            count = 0;

            using (var db = DbManager.CreateConnection(this.ConnectionName))
            {
                count = db.QueryFirst<int>(SqlIdentityText.Count);
                if (count == 0)
                {
                    return results;
                }

                results = db.Query<EmployeeViewModel>(SqlIdentityText.InnerJoinEmployee);
            }

            return results;
        }
    }
}