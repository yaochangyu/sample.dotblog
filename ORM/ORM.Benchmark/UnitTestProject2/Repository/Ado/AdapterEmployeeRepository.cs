using System.Configuration;
using System.Data;
using System.Data.Common;

namespace UnitTestProject2.Repository.Ado
{
    public class AdapterEmployeeRepository : IAdoEmployeeRepository
    {
        private readonly DbDataAdapter _dataAdapter;

        public AdapterEmployeeRepository(string connectionName)
        {
            this.ConnectionName = connectionName;
            var providerName = ConfigurationManager.ConnectionStrings[connectionName].ProviderName;

            DbProviderFactory factory = DbProviderFactories.GetFactory(providerName);
            this._dataAdapter = factory.CreateDataAdapter();
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
            var result = new DataTable();
            this._dataAdapter.Fill(result);
            return result;
        }
    }
}