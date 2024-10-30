namespace Lab.Sharding.DB;

public interface IConnectionStringProvider
{
	string GetConnectionString(string databaseIdentifier);

	void SetConnectionStrings(Dictionary<string, string> connectionStrings);
}

public class ConnectionStringProvider : IConnectionStringProvider
{
	private Dictionary<string, string> _connectionStrings;

	public ConnectionStringProvider()
	{
	
	}

	public void SetConnectionStrings(Dictionary<string, string> connectionStrings)
	{
		this._connectionStrings = connectionStrings;
	}

	public string GetConnectionString(string databaseIdentifier)
	{
		return this._connectionStrings.TryGetValue(databaseIdentifier, out var connectionString)
			? connectionString
			: throw new ArgumentException("Unknown database identifier");
	}
}