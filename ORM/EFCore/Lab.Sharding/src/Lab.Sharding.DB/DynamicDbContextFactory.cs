namespace Lab.Sharding.DB;

public interface IDynamicDbContextFactory<TContext>
	where TContext : DynamicDbContext
{
	TContext CreateDbContext(string serverName,string databaseName, string tablePostfix);
}

public class DynamicDbContextFactory<TContext>(IConnectionStringProvider connectionStringProvider)
	: IDynamicDbContextFactory<TContext>
	where TContext : DynamicDbContext
{
	public TContext CreateDbContext(string serverName,string databaseName, string tablePostfix)
	{
		var connectionString = connectionStringProvider.GetConnectionString(serverName, databaseName);
		return (TContext)Activator.CreateInstance(typeof(TContext), connectionString, tablePostfix);
	}
}