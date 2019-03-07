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
            var totalCount = 0;
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
	[p].[SequenceId],
	[Identity].[Account],
	[Identity].[Password]
FROM
	[dbo].[Employee] [p]
		LEFT JOIN [dbo].[Identity] [Identity] ON [p].[Id] = [Identity].[Employee_Id]";

            using (var db = DbManager.CreateConnection(ConnectionName))
            {
                totalCount = db.QueryFirst<int>(countText);
                if (totalCount == 0)
                {
                    return results;
                }

                results = db.Query<EmployeeViewModel>(selectText);
            }
            return results;
        }
    }
}