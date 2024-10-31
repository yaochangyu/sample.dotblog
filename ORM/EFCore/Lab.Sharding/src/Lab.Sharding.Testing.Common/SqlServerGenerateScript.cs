using Microsoft.Data.SqlClient;

namespace Lab.Sharding.Testing.Common;

public class SqlServerGenerateScript
{
	public static string ClearAllRecord()
	{
		return @"
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'
EXEC sp_MSForEachTable 'SET QUOTED_IDENTIFIER ON; DELETE FROM ?'
EXEC sp_MSForEachTable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'
";
	}

	public static void OnlySupportLocal(string connectionString)
	{
		var allowData = new List<string>
		{
			"localhost", "127.0.0.1", "172.17.0.1" //localhost in docker
		};
		var builder = new SqlConnectionStringBuilder(connectionString);
		var dataSource = builder.DataSource.Split(',')[0]; // Extract the IP part
		var contains = allowData.Contains(dataSource);
		if (contains == false)
		{
			throw new NotSupportedException($"伺服器只支援 localhost，目前連線字串為 {connectionString}");
		}
	}
}