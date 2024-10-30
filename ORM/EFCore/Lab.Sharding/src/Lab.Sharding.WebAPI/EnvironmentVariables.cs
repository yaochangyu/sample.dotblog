using Lab.Sharding.Infrastructure;

namespace Lab.Sharding.WebAPI;

public record ASPNETCORE_ENVIRONMENT : EnvironmentVariableBase;

public record SYS_DATABASE_CONNECTION_STRING1 : EnvironmentVariableBase;

public record SYS_DATABASE_CONNECTION_STRING2 : EnvironmentVariableBase;

public record SYS_REDIS_URL : EnvironmentVariableBase;

public record EXTERNAL_API : EnvironmentVariableBase;