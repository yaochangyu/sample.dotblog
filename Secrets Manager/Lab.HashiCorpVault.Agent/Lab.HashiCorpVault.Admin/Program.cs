using System.Text.Json.Nodes;

namespace Lab.HashiCorpVault.Admin;

class Program
{
    private static string VaultServer = "http://127.0.0.1:8200";
    private static string VaultRootToken = "你的 Vault Root Token，這裡替換掉";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Setup Vault Starting...");

        var setup = new VaultAgentSetup(VaultServer, VaultRootToken);
        await setup.SetupVaultAgentAsync();
        // 建立管理者 Token
        var adminTokens = await setup.CreateAdminToken();
        
        // 使用管理者 Token 取得 app-dev 的 AppRole 認證資訊
        string adminToken = adminTokens["app-admin"];
        var id = await setup.GetAppRoleCredentialsAsync(adminToken, "app-dev");
        
        Console.WriteLine($"Role ID: {id.RoleId}, Secret ID: {id.SecretId}");
        // 印出管理者 Token
        string tokenString = string.Join(", ", adminTokens.Select(t => $"{t.Key}: {t.Value}"));
        Console.WriteLine($"Token: {tokenString}");
        Console.WriteLine("Setup Vault Completed.");
    }
}
