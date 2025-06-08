using Lab.HashiCorpVault.Client;

namespace Lab.HashiCorpVault.Admin;

public class VaultAppRoleSetup2
{
    private readonly string _vaultServer;
    private readonly VaultApiClient _vaultApiClient;
    private const string AppRoleName = "app-dev";
    private const string SecretRootPathName = "dev";

    public readonly List<Secret> Secrets =
    [
        new Secret("Account", "root"),
        new Secret("Password", "123456"),
    ];

    public readonly Dictionary<string, string> AdminPolicies = new()
    {
        ["app-admin"] = """
                        # 允許為特定 AppRole 產生 secret id/role id
                        path "auth/approle/role/+/secret-id" {
                          capabilities = ["create", "update"]
                        }

                        path "auth/approle/role/+/role-id" {
                          capabilities = ["read"]
                        }
                        """
    };

    public readonly Dictionary<string, string> SecretPolicies = new()
    {
        [AppRoleName] = $$"""
                          path "{{SecretRootPathName}}/data/db/connection/*" {
                            capabilities = ["read"]
                          }
                          """,
    };

    public VaultAppRoleSetup2(VaultApiClient vaultApiClient)
    {
        _vaultApiClient = vaultApiClient;
    }

    public async Task SetupVaultAsync()
    {
        // 啟用 KV v2 秘密引擎和 AppRole 認證方法
        await EnableAppRoleAsync();
        await EnableSecretAsync();

        // 建立機敏性資料
        await SetupSecretAsync();

        // 建立 Client 的 AppRole 和 Policies
        await SetupPolicesAsync(SecretPolicies);
        await SetupAppRoleAsync(SecretPolicies);

        // 建立 Admin 的 AppRole 和 Policies
        await SetupPolicesAsync(AdminPolicies);
        await SetupAppRoleAsync(AdminPolicies);
    }

    public async Task<Dictionary<string, string>> CreateAdminToken()
    {
        var policies = AdminPolicies;

        // 產生 Admin Token
        var results = new Dictionary<string, string>();
        foreach (var policy in policies)
        {
            Console.WriteLine("建立 Admin Token...");
            string policyName = policy.Key;
            var tokenResult = await _vaultApiClient.CreateTokenAsync(policyName, "8760h"); // 360d = 8760h
            var clientToken = tokenResult["auth"]?["client_token"]?.GetValue<string>();
            results.Add(policyName, clientToken);
            Console.WriteLine("Admin Token 建立完成");
        }

        return results;
    }

    public async Task<(string RoleId, string SecretId)> GetAppRoleCredentialsAsync(string adminToken, string roleName)
    {
        // // 更新 token
        // _vaultApiClient.UpdateToken(adminToken);

        // 取得 Role ID
        var roleResult = await _vaultApiClient.GetRoleIdAsync(roleName);
        var roleId = roleResult["data"]?["role_id"]?.GetValue<string>();

        // 取得 Secret ID
        var secretResult = await _vaultApiClient.GenerateSecretIdAsync(roleName);
        var secretId = secretResult["data"]?["secret_id"]?.GetValue<string>();

        // // 恢复原始 token
        // _vaultApiClient.UpdateToken(_rootToken);

        return (roleId, secretId);
    }

    private async Task SetupSecretAsync()
    {
        Console.WriteLine("Writing secrets to kv v2...");
        var secretData = Secrets.ToDictionary(s => s.Key, s => s.Value);
        var writeSecretResult = await _vaultApiClient.WriteSecretAsync($"{SecretRootPathName}/data/db/connection/identity", secretData);
        Console.WriteLine("kv v2 completed");
    }

    private async Task EnableSecretAsync()
    {
        try
        {
            Console.WriteLine("啟用 KV v2 秘密引擎...");
            var enableSecretEngineAsync = await _vaultApiClient.EnableSecretEngineAsync("kv-v2", SecretRootPathName);
            Console.WriteLine("KV v2 秘密引擎啟用完成");
        }
        catch (Exception e)
        {
            Console.WriteLine($"KV v2 可能已經啟用: {e.Message}");
        }
    }

    private async Task SetupPolicesAsync(Dictionary<string, string> policies)
    {
        // 建立政策
        foreach (var (policyName, policyContent) in policies)
        {
            Console.WriteLine($"建立 {policyName} 政策...");
            var writePolicyResult = await _vaultApiClient.WritePolicyAsync(policyName, policyContent);
            Console.WriteLine($"政策 {policyName} 建立完成");
        }
    }

    private async Task EnableAppRoleAsync()
    {
        try
        {
            Console.WriteLine("啟用 AppRole 認證方法...");
            var enableAuthMethodResult = await _vaultApiClient.EnableAuthMethodAsync("approle");
            Console.WriteLine("AppRole 認證方法啟用完成");
        }
        catch (Exception e)
        {
            Console.WriteLine($"AppRole 可能已經啟用: {e.Message}");
        }
    }

    private async Task SetupAppRoleAsync(Dictionary<string, string> policies)
    {
        foreach (var (roleName, _) in policies)
        {
            Console.WriteLine($"建立 {roleName} 的 AppRole...");
            var createAppRoleResult = await _vaultApiClient.CreateAppRoleAsync(
                roleName: roleName,
                policies: roleName,
                tokenTtl: "1h",
                tokenMaxTtl: "4h"
            );
            Console.WriteLine($"{roleName} 的 AppRole 建立完成");
        }
    }
}
