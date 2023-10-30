using Lab.Snapshot.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.Snapshot.Test;

public class ServiceConfiguration
{
    public static void ConfigDb(IServiceCollection services)
    {
        services.AddSingleton(p => { return LoggerFactory.Create(builder => { builder.AddConsole(); }); });
        services.AddDbContextFactory<MemberDbContext>((p, options) =>
        {
            var connectionString = Environment.GetEnvironmentVariable(EnvironmentNames.DbConnectionString);
            options.UseNpgsql(connectionString,
                    builder => builder.EnableRetryOnFailure(
                        10,
                        TimeSpan.FromSeconds(30),
                        new List<string> { "57P01" }))
                ;
        });
    }
}