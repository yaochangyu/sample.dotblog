using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using Dapper;
using UnitTestProject2.Repository.Ef.EntityModel;
using UnitTestProject2.ViewModel;

namespace UnitTestProject2.Repository.Ado
{
    public class DataReaderEmployeeRepository : IEmployeeRepository
    {
        public DataReaderEmployeeRepository(string connectionName)
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

            using (var dbConnection = DbManager.CreateConnection(ConnectionName))
            using (var dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandType = CommandType.Text;

                dbCommand.CommandText = countText;
                count = (int)dbCommand.ExecuteScalar();

                dbCommand.CommandText = selectText;
                var reader = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                while (reader.Read())
                {
                    var items = new object[reader.FieldCount];
                    reader.GetValues(items);
                }
            }
            return results;
        }
    }
}