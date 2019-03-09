using System.Data;

namespace UnitTestProject2.Repository.Ado
{
    public class DataReaderToTableEmployeeRepository : IAdoEmployeeRepository
    {
        public DataReaderToTableEmployeeRepository(string connectionName)
        {
            this.ConnectionName = connectionName;
        }

        public string ConnectionName { get; set; }

        public DataTable GetAllEmployees(out int count)
        {
            DataTable result = null;

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

            using (var dbConnection = DbManager.CreateConnection(this.ConnectionName))
            using (var dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandType = CommandType.Text;

                dbCommand.CommandText = SqlIdentityText.Count;
                count = (int) dbCommand.ExecuteScalar();
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
            var result = DbManager.CreateTable(reader);
            while (reader.Read())
            {
                DataRow row = result.NewRow();
                for (int i = 0; i < result.Columns.Count; i++)
                {
                    row[result.Columns[i]] = reader[i];
                }

                result.Rows.Add(row);
            }

            return result;
        }
    }
}