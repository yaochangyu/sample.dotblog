using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics.Contracts;

namespace UnitTestProject2
{
    public class DbManager
    {

        public static DbConnection CreateConnection(string connectionStringName)
        {

            var connectSetting = ConfigurationManager.ConnectionStrings[connectionStringName];

            if (connectSetting == null)
            {
                throw new ArgumentNullException(connectionStringName + " not exist at app.config");
            }

            var factory = DbProviderFactories.GetFactory(connectSetting.ProviderName);
            var connection = factory.CreateConnection();

            //TODO:連線字串解密

            connection.ConnectionString = connectSetting.ConnectionString;
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            return connection;
        }
        public static DbDataAdapter CreateDataAdapter(DbConnection connection)
        {
            return DbProviderFactories.GetFactory(connection).CreateDataAdapter();
        }

        public static DataSet ExecuteDataSet(DbCommand command)
        {
            string @namespace = command.Connection.GetType().Namespace;
            DbProviderFactory factory = DbProviderFactories.GetFactory(@namespace);
            var adapter = factory.CreateDataAdapter();
            adapter.SelectCommand = command;
            var result = new DataSet();
            adapter.Fill(result);
            return result;
        }

        public static DataTable ExecuteDataTable(DbCommand command)
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