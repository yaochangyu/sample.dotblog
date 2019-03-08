using System;
using System.Collections.Generic;
using System.Data;

namespace UnitTestProject2.Repository.Ado
{
    public class DataReaderEmployeeRepository : IAdoEmployeeRepository
    {
        public DataReaderEmployeeRepository(string connectionName)
        {
            this.ConnectionName = connectionName;
        }

        public string ConnectionName { get; set; }

        public DataTable GetAllEmployees(out int count)
        {
            DataTable result = null;
            var totalCount = 0;
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

            using (var dbConnection = DbManager.CreateConnection(this.ConnectionName))
            using (var dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandType = CommandType.Text;

                dbCommand.CommandText = countText;
                count = (int) dbCommand.ExecuteScalar();
                if (count == 0)
                {
                    return result;
                }

                dbCommand.CommandText = selectText;
                var reader = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                DataTable schema = reader.GetSchemaTable();
                result = new DataTable();
                List<DataColumn> columns = new List<DataColumn>();
                if (schema != null)
                {
                    foreach (DataRow row in schema.Rows)
                    {
                        string columnName = Convert.ToString(row["ColumnName"]);
                        DataColumn column = new DataColumn(columnName, (Type) row["DataType"]);
                        column.Unique = (bool) row["IsUnique"];
                        column.AllowDBNull = (bool) row["AllowDBNull"];
                        column.AutoIncrement = (bool) row["IsAutoIncrement"];
                        columns.Add(column);
                        result.Columns.Add(column);
                    }
                }

                while (reader.Read())
                {
                    DataRow row = result.NewRow();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        row[columns[i]] = reader[i];
                    }

                    result.Rows.Add(row);
                }
            }

            return result;
        }
    }
}