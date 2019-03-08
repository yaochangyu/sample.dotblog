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

            using (var dbConnection = DbManager.CreateConnection(this.ConnectionName))
            using (var dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandType = CommandType.Text;

                dbCommand.CommandText = SqlEmployeeText.AllEmployee;
                var reader = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                result = ToDataTable(reader);
                count = result.Rows.Count;
            }

            return result;
        }

        public DataTable GetAllEmployeesDetail(out int count)
        {
            DataTable result = null;
            var totalCount = 0;
            count = 0;

            using (var dbConnection = DbManager.CreateConnection(this.ConnectionName))
            using (var dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandType = CommandType.Text;

                dbCommand.CommandText = SqlIdentityText.Count;
                count = (int)dbCommand.ExecuteScalar();
                if (count == 0)
                {
                    return result;
                }

                dbCommand.CommandText = SqlIdentityText.InnerJoinEmployee;
                var reader = dbCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                result = ToDataTable(reader);
            }

            return result;
        }

        private static DataTable ToDataTable(IDataReader reader)
        {
            DataTable result;
            //DataTable schema = reader.GetSchemaTable();
            //result = new DataTable();
            //List<DataColumn> columns = new List<DataColumn>();
            //if (schema != null)
            //{
            //    foreach (DataRow row in schema.Rows)
            //    {
            //        string columnName = Convert.ToString(row["ColumnName"]);
            //        DataColumn column = new DataColumn(columnName, (Type)row["DataType"]);
            //        column.Unique = (bool)row["IsUnique"];
            //        column.AllowDBNull = (bool)row["AllowDBNull"];
            //        column.AutoIncrement = (bool)row["IsAutoIncrement"];
            //        columns.Add(column);
            //        result.Columns.Add(column);
            //    }
            //}
            result = TableUtility.GetEmployeeTable();
            var columns = result.Columns;
            while (reader.Read())
            {
                DataRow row = result.NewRow();
                for (int i = 0; i < columns.Count; i++)
                {
                    row[columns[i]] = reader[i];
                }

                result.Rows.Add(row);
            }

            return result;
        }
    }
}