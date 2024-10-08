using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SecretsEngines.Database;
using VaultSharp.V1.SecretsEngines.Database.Models.PostgreSQL;

namespace Lab.HashiCorpVault.Test;

[TestClass]
public class DynamicCredentialsTest
{
    private readonly string VaultToken = "hvs.Wst5O7xSTLt8lMzAmtsB3vG1";
    private readonly string VaultServer = "http://127.0.0.1:8200";

    [TestMethod]
    public async Task EnableDatabaseEngine()
    {
        var vaultClient = this.CreateVaultClient();

        var enableSecretsEngine = new SecretsEngine
        {
            Type = new SecretsEngineType("database"),
            Path = "database",
            Description = "Database Secrets Engine"
        };

        await vaultClient.V1.System.MountSecretBackendAsync(enableSecretsEngine);
        Console.WriteLine("Secrets 已成功寫入！");
    }

    [TestMethod]
    public async Task ConfigureConnectionAsync()
    {
        var vaultClient = this.CreateVaultClient();

        // 寫入配置到 Vault
        var config = new PostgreSQLConnectionConfigModel
        {
            PluginName = "postgresql-database-plugin",
            AllowedRoles = new List<string> { "my-db-role" },
            Username = "user",
            Password = "password",
            ConnectionUrl = "postgresql://{{username}}:{{password}}@localhost:5432/postgres?sslmode=disable",

            // 正確的 username_template 使用有效的模板函數
            UsernameTemplate = "{{uuid}}",
        };
        var connectionName = "my-postgresql-database";

        await vaultClient.V1.Secrets.Database.ConfigureConnectionAsync(connectionName, config);

        Console.WriteLine("已成功寫入 PostgreSQL 配置！");
    }

    [TestMethod]
    public async Task CreateRoleAsync()
    {
        var vaultClient = this.CreateVaultClient();

        // 定義創建語句
        var creationStatements = @"
CREATE ROLE ""{{name}}"" WITH LOGIN PASSWORD '{{password}}' VALID UNTIL '{{expiration}}';
GRANT SELECT ON ALL TABLES IN SCHEMA public TO ""{{name}}"";
";
        var connectionName = "my-postgresql-database";

        var role = new Role()
        {
            DatabaseProviderType = new DatabaseProviderType(connectionName),
            DefaultTimeToLive = "1h",
            MaximumTimeToLive = "24h",
            CreationStatements = [creationStatements],
            RevocationStatements = null,
            RollbackStatements = null,
            RenewStatements = null,
        };
        var roleName = "my-db-role";
        await vaultClient.V1.Secrets.Database.CreateRoleAsync(roleName, role);

        Console.WriteLine("已成功寫入資料庫角色配置！");
    }

    [TestMethod]
    public async Task ReadRoleAsync()
    {
        var vaultClient = this.CreateVaultClient();

        // 讀取資料庫角色的詳細資訊
        var roleName = "my-db-role";
        var roleInfo = await vaultClient.V1.Secrets.Database.ReadRoleAsync(roleName);

        // 輸出角色的詳細資訊

        var roleJson = JsonSerializer.Serialize(roleInfo);
        Console.WriteLine(roleJson);
    }

    [TestMethod]
    public async Task GetCredentialsAsync()
    {
        var vaultClient = this.CreateVaultClient();

        // 讀取資料庫角色的憑證
        var roleName = "my-db-role";
        var credentials = await vaultClient.V1.Secrets.Database.GetCredentialsAsync(roleName);

        // 輸出角色的詳細資訊

        var roleJson = JsonSerializer.Serialize(credentials);
        Console.WriteLine(roleJson);
    }

    [TestMethod]
    public async Task RenewLeaseAsync()
    {
        var vaultClient = this.CreateVaultClient();

        var leaseId = "database/creds/my-db-role/RhazfRMw84PiOJfFZD5QAEez";

        // 續期租約
        var renewedLease = await vaultClient.V1.System.RenewLeaseAsync(leaseId, 3600);
        Console.WriteLine("租約已成功續期！");
        var roleJson = JsonSerializer.Serialize(renewedLease);
        Console.WriteLine(roleJson);
    }

    [TestMethod]
    public async Task RevokeLeaseAsync()
    {
        var vaultClient = this.CreateVaultClient();

        var leaseId = "database/creds/my-db-role/RhazfRMw84PiOJfFZD5QAEez";

        // 續期租約
        await vaultClient.V1.System.RevokeLeaseAsync(leaseId);
        Console.WriteLine("租約已成功撤銷！");
    }

    [TestMethod]
    public async Task GetAllLeasesAsync()
    {
        var vaultClient = this.CreateVaultClient();


        // 續期租約
        
        var secrets = await vaultClient.V1.System.GetAllLeasesAsync("");
        Console.WriteLine("租約已成功撤銷！");
    }

    private VaultClient CreateVaultClient()
    {
        var authMethod = new TokenAuthMethodInfo(this.VaultToken);
        var vaultClientSettings = new VaultClientSettings(this.VaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);
        return vaultClient;
    }
}