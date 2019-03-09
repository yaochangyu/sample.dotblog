using System.Data;
using System.Data.Common;

namespace UnitTestProject2.Repository.Ado
{
    public class AdapterEmployeeRepository : IAdoEmployeeRepository
    {
        public AdapterEmployeeRepository(string connectionName)
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
                result = this.ExecuteDataTable(dbCommand);
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
                result = this.ExecuteDataTable(dbCommand);
            }

            return result;
        }

        public DataTable ExecuteDataTable(DbCommand command)
        {
            string @namespace = command.Connection.GetType().Namespace;
            DbProviderFactory factory = DbProviderFactories.GetFactory(@namespace);
            var adapter = factory.CreateDataAdapter();
            adapter.SelectCommand = command;
            var result = new DataTable();
            adapter.Fill(result);
            return result;
        }
    }
}