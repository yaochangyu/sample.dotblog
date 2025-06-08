using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Lab.HashiCorpVault.Admin;

record Secret(string Key, string Value);

public class VaultAgentSetup
{
    private readonly string _vaultServer;
    private readonly string _rootToken;
    private const string AppRoleName = "app-dev";
    private List<Secret> Secrets { get; set; } =
    [
        new Secret("Account", "root"),
        new Secret("Password", "123456"),
    ];

    private readonly Dictionary<string, string> _adminPolicies = new()
    {
        ["app-admin"] = """
                        # 允許為特定 AppRole 產生 secret id/role id
                        path "auth/approle/role/app-dev/secret-id" {
                          capabilities = ["create", "update"]
                        }

                        path "auth/approle/role/app-dev/role-id" {
                          capabilities = ["read","list","create","update"]
                        }
                        """
    };


    private readonly Dictionary<string, string> _secretPolicies = new()
    {
        [AppRoleName] = """
                        path "dev/data/db/connection/*" {
                          capabilities = ["read"]
                        }

                        path "auth/approle/role/app-dev/role-id" {
                          capabilities = ["read"]
                        }
                        """,
    };

    public VaultAgentSetup(string vaultServer, string rootToken)
    {
        _vaultServer = vaultServer;
        _rootToken = rootToken;

        // 設定環境變數
        Environment.SetEnvironmentVariable("VAULT_ADDR", _vaultServer);
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _rootToken);
    }

    public async Task SetupVaultAsync()
    {
        // 建立機敏性資料
        await SetupSecretAsync();
        
        // 建立 Client 的 AppRole 和 Policies
        await SetupAppRoleAsync(_secretPolicies);
        await SetupPolicesAsync(_secretPolicies);

        // 建立 Admin 的 AppRole 和 Policies
        await SetupAppRoleAsync(_adminPolicies);
        await SetupPolicesAsync(_adminPolicies);
    }

    public async Task<Dictionary<string, string>> CreateAdminToken()
    {
        var policies = _adminPolicies;

        // 產生 Admin Token
        var results = new Dictionary<string, string>();
        foreach (var policy in policies)
        {
            Console.WriteLine("建立 Admin Token...");
            string policyName = policy.Key;
            var tokenResult = await ExecuteVaultCommandAsync($"token create -policy={policyName} -format=json -ttl=360d");
            var tokenJsonObject = JsonNode.Parse(tokenResult).AsObject();
            var clientToken = tokenJsonObject["auth"]?["client_token"]?.GetValue<string>();
            results.Add(policyName, clientToken);
            Console.WriteLine("Admin Token 建立完成");
        }

        return results;
    }

    public async Task<(string RoleId, string SecretId)> GetAppRoleCredentialsAsync(string adminToken, string roleName)
    {
        Environment.SetEnvironmentVariable("VAULT_TOKEN", adminToken);
        // 取得 Role ID
        var roleResult = await ExecuteVaultCommandAsync($"read -format=json auth/approle/role/{roleName}/role-id");
        var roleJsonObject = JsonNode.Parse(roleResult).AsObject();
        var roleId = roleJsonObject["data"]?["role_id"]?.GetValue<string>();

        // 取得 Secret ID
        var secretResult = await ExecuteVaultCommandAsync($"write -format=json -f auth/approle/role/{roleName}/secret-id");
        var secretJsonObject = JsonNode.Parse(secretResult).AsObject();
        var secretId = secretJsonObject["data"]?["secret_id"]?.GetValue<string>();
        return (roleId, secretId);
    }

    private async Task SetupSecretAsync()
    {
        try
        {
            Console.WriteLine("啟用 KV v2 秘密引擎...");
            await ExecuteVaultCommandAsync("secrets enable -path=dev kv-v2");
        }
        catch (Exception e)
        {
            Console.WriteLine($"KV v2 可能已經啟用: {e.Message}");
        }
        Console.WriteLine("Writing secrets to kv v2...");

        string secret = string.Join(" ", Secrets.Select(s => $"{s.Key}={s.Value}"));
        await ExecuteVaultCommandAsync($"kv put dev/db/connection/identity {secret}");
    }

    private async Task SetupPolicesAsync(Dictionary<string, string> policies)
    {
        // 建立政策
        foreach (var (policyName, policyContent) in policies)
        {
            Console.WriteLine($"建立 {policyName} 政策...");
            string policyFile = $"{policyName}-policy.hcl";
            await File.WriteAllTextAsync(policyFile, policyContent);
            await ExecuteVaultCommandAsync($"policy write {policyName} {policyFile}");
            File.Delete(policyFile);
            Console.WriteLine($"政策 {policyName} 建立完成");
        }
    }

    private async Task SetupAppRoleAsync(Dictionary<string, string> policies)
    {
        try
        {
            Console.WriteLine("啟用 AppRole 認證方法...");
            await ExecuteVaultCommandAsync("auth enable approle");
        }
        catch (Exception e)
        {
            Console.WriteLine($"AppRole 可能已經啟用: {e.Message}");
        }

        foreach (var (roleName, _) in policies)
        {
            Console.WriteLine($"建立 {roleName} 的 AppRole...");
            await ExecuteVaultCommandAsync($"write auth/approle/role/{roleName} " +
                                           $"token_policies={roleName} " +
                                           $"token_ttl=1h " +
                                           $"token_max_ttl=4h");
            Console.WriteLine($"{roleName} 的 AppRole 建立完成");
        }
    }

    private async Task<string> ExecuteVaultCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "vault",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Vault command failed: {error}");
        }

        return output;
    }
}
