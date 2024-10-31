namespace Lab.Sharding.DB;

public interface IConnectionStringProvider
{
	string GetConnectionString(string serverName, string databaseName);

	void SetConnectionStrings(Dictionary<string, string> connectionStrings);
}

public class ConnectionStringProvider : IConnectionStringProvider
{
	private Dictionary<string, string> _connectionStrings;

	public void SetConnectionStrings(Dictionary<string, string> connectionStrings)
	{
		this._connectionStrings = connectionStrings;
	}

	public string GetConnectionString(string serverName, string databaseName)
	{
		if (this._connectionStrings.TryGetValue(serverName, out var connectionString) == false)
		{
			throw new ArgumentException("Unknown database identifier");
		}

		connectionString = $"{connectionString};Database={databaseName}";

		return connectionString;
	}
}