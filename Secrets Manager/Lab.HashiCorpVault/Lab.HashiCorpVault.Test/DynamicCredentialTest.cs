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
    private readonly string VaultToken = "你的驗證";
    private readonly string VaultServer = "http://127.0.0.1:8200";

    [TestMethod]
    public async Task _01_啟用資料庫()
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
    public async Task _02_配置資料庫連線()
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
    public async Task _03_建立角色()
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
    public async Task _04_取得角色資訊()
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
    public async Task _05_建立憑證()
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
    public async Task _06_續約憑證()
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
    public async Task _07_撤銷憑證()
    {
        var vaultClient = this.CreateVaultClient();

        var leaseId = "database/creds/my-db-role/RhazfRMw84PiOJfFZD5QAEez";

        // 續期租約
        await vaultClient.V1.System.RevokeLeaseAsync(leaseId);
        Console.WriteLine("租約已成功撤銷！");
    }
    
    [TestMethod]
    public async Task _08_取得所有憑證()
    {
        // 初始化 Vault Client
        var vaultClient = this.CreateVaultClient();

        // 指定要查詢的角色名稱
        var roleName = "my-db-role";

        // var lease = await vaultClient.V1.System.GetLeaseAsync("database/creds/my-db-role/cpMIwcic5bwm0yTcI8UE5OUy");

        try
        {
            var leases = await vaultClient.V1.System.GetAllLeasesAsync("database/creds/" + roleName);
            var json = JsonSerializer.Serialize(leases);
            Console.WriteLine(json);
        }
        catch (VaultApiException e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private VaultClient CreateVaultClient()
    {
        var authMethod = new TokenAuthMethodInfo(this.VaultToken);
        var vaultClientSettings = new VaultClientSettings(this.VaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);
        return vaultClient;
    }
}