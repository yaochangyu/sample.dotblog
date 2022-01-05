namespace Lab.EFCoreBulk;

public class AppEnvironmentOption
{
    public string EmployeeDbConnectionString
    {
        get
        {
            if (string.IsNullOrWhiteSpace(this._memberDbConnectionString))
            {
                this._memberDbConnectionString =
                    EnvironmentAssistant.GetEnvironmentVariable(this.EMPLOYEE_DB_CONN_STR);
            }

            return this._memberDbConnectionString;
        }
        set
        {
            this._memberDbConnectionString = value;
            Environment.SetEnvironmentVariable(this.EMPLOYEE_DB_CONN_STR, value);
        }
    }

    private string _crmDbConnectionString;
    private string _currentMarket;
    private string _memberApiToken;
    private string _memberDbConnectionString;
    private string _memberServiceBaseEndpoint;
    private string _webStoreDbConnectionString;
    private readonly string EMPLOYEE_DB_CONN_STR = "EMPLOYEE_DB_CONNECTION_STR";

    public void Initial()
    {
        var memberDbConnectionString = this.EmployeeDbConnectionString;
    }
}