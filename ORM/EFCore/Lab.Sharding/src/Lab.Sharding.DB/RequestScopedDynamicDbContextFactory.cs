using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lab.Sharding.DB;

public static class HttpContextExtensions
{
	public static T GetOrCreate<T>(this IDictionary<object, object?> items, object key, Func<T> factory)
		where T : class
	{
		if (items.TryGetValue(key, out var existingValue) && existingValue is T existing)
		{
			return existing;
		}

		var result = factory();
		items[key] = result;
		return result;
	}
}

public class RequestScopedDynamicDbContextFactory<TContext>(
	IConnectionStringProvider connectionStringProvider,
	IHttpContextAccessor httpContextAccessor)
	: IDynamicDbContextFactory<TContext>
	where TContext : DynamicDbContext
{
	public TContext CreateDbContext(string serverName, string databaseName, string tablePostfix)
	{
		var httpContext = httpContextAccessor.HttpContext;
		if (httpContext == null)
		{
			throw new InvalidOperationException("HttpContext is not available");
		}

		var name = (typeof(RequestScopedDynamicDbContextFactory<TContext>)).Name;
		var contextDictionary = httpContext.Items.GetOrCreate(
			name,
			() => new ConcurrentDictionary<string, TContext>());

		var requestId = httpContext.Items.GetOrCreate("RequestId", () => Guid.NewGuid().ToString());

		return contextDictionary.GetOrAdd(requestId, key =>
		{
			var connectionString = connectionStringProvider.GetConnectionString(serverName, databaseName);

			// var scope = serviceScopeFactory.CreateScope();
			var scope = httpContextAccessor.HttpContext.RequestServices.CreateScope();

			// 在請求結束時釋放資源
			httpContext.Response.RegisterForDispose(scope);

			var options = new DbContextOptionsBuilder<TContext>()
				.UseSqlServer(connectionString)
				.Options;

			return (TContext)Activator.CreateInstance(
				typeof(TContext),
				connectionString,
				tablePostfix);
		});
	}
}