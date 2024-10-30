using Microsoft.EntityFrameworkCore;

namespace Lab.Sharding.DB;

public interface IDynamicDbContextFactory<TContext>
	where TContext : DbContext
{
	TContext CreateDbContext(string connectionString);
}

public class DynamicDbContextFactory<TContext> : IDynamicDbContextFactory<TContext>
	where TContext : DbContext
{
	private readonly DbContextOptionsBuilder<DbContext> _optionsBuilder;
	private readonly IConnectionStringProvider _connectionStringProvider;

	public DynamicDbContextFactory(IConnectionStringProvider connectionStringProvider)
	{
		this._connectionStringProvider = connectionStringProvider;
		this._optionsBuilder = new DbContextOptionsBuilder<DbContext>();
	}

	public TContext CreateDbContext(string databaseIdentifier)
	{
		var connectionString = this._connectionStringProvider.GetConnectionString(databaseIdentifier);
		this._optionsBuilder.UseSqlServer(connectionString);
		return (TContext)Activator.CreateInstance(typeof(TContext), _optionsBuilder.Options);
	}
}