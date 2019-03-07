using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        public IEnumerable<EmployeeViewModel> GetAllEmployees(out int count)
        {
            IEnumerable<EmployeeViewModel> results = null;

            count = 0;
            var countText = @"
SELECT
	Count(*)
FROM
	[dbo].[Identity] [p]
";
            var selectText = @"
SELECT
	[a_Employee].[Id],
	[a_Employee].[Name],
	[a_Employee].[Age],
	[a_Employee].[SequenceId],
	[p].[Account],
	[p].[Password]
FROM
	[dbo].[Identity] [p]
		INNER JOIN [dbo].[Employee] [a_Employee] ON [p].[Employee_Id] = [a_Employee].[Id]
ORDER BY
	[a_Employee].[SequenceId]";

            using (var db = DbManager.CreateConnection(ConnectionName))
            {
                count = db.QueryFirst<int>(countText);
                if (count == 0)
                {
                    return results;
                }

                results = db.Query<EmployeeViewModel>(selectText);
            }
            return results;
        }
    }
}