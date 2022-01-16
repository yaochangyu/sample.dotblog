namespace Lab.NETMiniProfiler.Infrastructure.EFCore6;

public enum DatabaseType
{
    MsSql = 1,
    PostgresSQL = 2
}

public class AppEnvironmentOption
{
    public DatabaseType DatabaseType
    {
        get
        {
            if (this._databaseType.HasValue == false)
            {
                var variable = EnvironmentAssistant.GetEnvironmentVariable(this.DATABASE_TYPE);
                if (Enum.TryParse(variable,true, out DatabaseType result))
                {
                    this._databaseType = result;
                }
            }

            return this._databaseType.Value;
        }
        set => this._databaseType = value;
    }

    public string EmployeeDbConnectionString
    {
        get
        {
            if (string.IsNullOrWhiteSpace(this._employeeDbConnectionString))
            {
                this._employeeDbConnectionString =
                    EnvironmentAssistant.GetEnvironmentVariable(this.EMPLOYEE_DB_CONN_STR);
            }

            return this._employeeDbConnectionString;
        }
        set
        {
            this._employeeDbConnectionString = value;
            Environment.SetEnvironmentVariable(this.EMPLOYEE_DB_CONN_STR, value);
        }
    }

    private readonly string DATABASE_TYPE = "DB_TYPE";
    private readonly string EMPLOYEE_DB_CONN_STR = "EMPLOYEE_DB_CONNECTION_STR";
    private DatabaseType? _databaseType;

    private string _employeeDbConnectionString;
}