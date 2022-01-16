namespace Lab.NETMiniProfiler.Infrastructure.EFCore5;

public class AppEnvironmentOption
{
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

    private string _employeeDbConnectionString;
    private readonly string EMPLOYEE_DB_CONN_STR = "EMPLOYEE_DB_CONNECTION_STR";

    public void Initial()
    {
        var memberDbConnectionString = this.EmployeeDbConnectionString;
    }
}