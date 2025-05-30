using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Lab.HashiCorpVault.Test;

public class VaultAppRoleSetup
{
    private readonly string _vaultServer;
    private readonly string _rootToken;

    public VaultAppRoleSetup(string vaultServer, string rootToken)
    {
        _vaultServer = vaultServer;
        _rootToken = rootToken;
    }
    private readonly Dictionary<string, string> _adminPolicies = new()
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

    private readonly Dictionary<string, string> _clientPolicies = new()
    {
        ["app-dev"] = """
                      path "dev/data/db/connection/*" {
                        capabilities = ["read"]
                      }

                      path "auth/approle/role/app-dev/role-id" {
                        capabilities = ["read"]
                      }
                      """,
        ["app-prod"] = """
                       path "prod/data/db/connection/*" {
                         capabilities = ["read"]
                       }
                       path "auth/approle/role/app-dev/role-id" {
                         capabilities = ["read"]
                       }
                       """,

    };

    public async Task SetupVaultAsync()
    {
        // 設定環境變數
        Environment.SetEnvironmentVariable("VAULT_ADDR", _vaultServer);
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _rootToken);

        // =====================================================================================
        // 管理者的操作
        // =====================================================================================
        await AdminSetup();

        // 建立一個具有產生 secret id 權限的 admin token
        string adminToken = await CreateClientToken("app-admin", _rootToken);

        // =====================================================================================
        // 用戶端的操作
        // =====================================================================================
        // adminToken 需要保存在用戶端的環境
        Console.WriteLine("Read Secret Data...");
        foreach (var (roleName, _) in _clientPolicies)
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

            // 使用 AppRole 登入，並取得機敏性資料
            await LoginAppRole(roleName, roleId, secretId);
        }

        Console.WriteLine("Unwrap Read Secret Data...");
        foreach (var (roleName, _) in _clientPolicies)
        {
            Environment.SetEnvironmentVariable("VAULT_TOKEN", adminToken);

            // 使用 wrap-ttl 參數來包裝 Role ID
            var roleResult = await ExecuteVaultCommandAsync($"read -format=json -wrap-ttl=60s auth/approle/role/{roleName}/role-id");
            var roleJsonObject = JsonNode.Parse(roleResult).AsObject();
            var wrappedRoleToken = roleJsonObject["wrap_info"]?["token"]?.GetValue<string>();

            // 使用 wrap-ttl 參數來包裝 Secret ID
            var secretResult = await ExecuteVaultCommandAsync($"write -format=json -wrap-ttl=60s -f auth/approle/role/{roleName}/secret-id");
            var secretJsonObject = JsonNode.Parse(secretResult).AsObject();
            var wrappedSecretToken = secretJsonObject["wrap_info"]?["token"]?.GetValue<string>();

            // 解 Role ID（模擬客戶端操作）
            var unwrappedRoleResult = await ExecuteVaultCommandAsync($"unwrap -format=json {wrappedRoleToken}");
            var unwrappedRoleJson = JsonNode.Parse(unwrappedRoleResult).AsObject();
            var roleId = unwrappedRoleJson["data"]?["role_id"]?.GetValue<string>();

            // 解 Secret ID（模擬客戶端操作）
            var unwrappedSecretResult = await ExecuteVaultCommandAsync($"unwrap -format=json {wrappedSecretToken}");
            var unwrappedSecretJson = JsonNode.Parse(unwrappedSecretResult).AsObject();
            var secretId = unwrappedSecretJson["data"]?["secret_id"]?.GetValue<string>();

            // 使用 AppRole 登入，並取得機敏性資料
            await LoginAppRole(roleName, roleId, secretId);
        }
    }
    private async Task AdminSetup()
    {
        // 管理者啟用 kv v2 秘密引擎
        try
        {
            Console.WriteLine("Enabling kv v2 secret engine...");
            //await ExecuteVaultCommandAsync("secrets enable -path=secret kv-v2");
            await ExecuteVaultCommandAsync("secrets enable -path=dev kv-v2");
            await ExecuteVaultCommandAsync("secrets enable -path=prod kv-v2");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        // 管理者建立 Secrets
        Console.WriteLine("Writing secrets to kv v2...");

        await ExecuteVaultCommandAsync("kv put dev/db/connection/identity Account=user Password=1111");
        await ExecuteVaultCommandAsync("kv put prod/db/connection/identity Account=user Password=111111");

        try
        {
            // 管理者啟用 AppRole 認證方式
            Console.WriteLine("Enabling AppRole auth method...");
            await ExecuteVaultCommandAsync("auth enable approle");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        // 管理者建立 Policies
        Console.WriteLine("Creating AppRole policies...");
        foreach (var (policyName, policyContent) in _adminPolicies)
        {
            string policyFile = $"{policyName}-policy.hcl";
            await File.WriteAllTextAsync(policyFile, policyContent);
            await ExecuteVaultCommandAsync($"policy write {policyName} {policyFile}");
            File.Delete(policyFile);
        }

        // 管理者建立 Policies
        Console.WriteLine("Creating AppRole policies...");
        foreach (var (policyName, policyContent) in _clientPolicies)
        {
            string policyFile = $"{policyName}-policy.hcl";
            await File.WriteAllTextAsync(policyFile, policyContent);
            await ExecuteVaultCommandAsync($"policy write {policyName} {policyFile}");
            File.Delete(policyFile);
        }

        // 管理者建立 AppRoles
        Console.WriteLine("Creating AppRoles...");
        foreach (var (roleName, _) in _clientPolicies)
        {
            await ExecuteVaultCommandAsync($"write auth/approle/role/{roleName} token_policies={roleName} token_ttl=1h token_max_ttl=4h");
        }

        foreach (var (roleName, _) in _adminPolicies)
        {
            await ExecuteVaultCommandAsync($"write auth/approle/role/{roleName} token_policies={roleName} token_ttl=1h token_max_ttl=4h");
        }
    }

    private async Task<string> CreateClientToken(string policyName, string token)
    {
        Environment.SetEnvironmentVariable("VAULT_TOKEN", token);

        var tokenResult = await ExecuteVaultCommandAsync($"token create -policy={policyName} -format=json -ttl=360d");
        var tokenJsonObject = JsonNode.Parse(tokenResult).AsObject();
        var result = tokenJsonObject["auth"]?["client_token"]?.GetValue<string>();
        return result;
    }

    private async Task LoginAppRole(string roleName, string roleId, string secretId)
    {
        Console.WriteLine($"\nTesting AppRole login for {roleName}...");

        // 使用 AppRole 登入
        var tokenResult = await ExecuteVaultCommandAsync($"write -format=json auth/approle/login role_id={roleId} secret_id={secretId}");
        var tokenJsonObject = JsonNode.Parse(tokenResult).AsObject();
        var clientToken = tokenJsonObject["auth"]?["client_token"]?.GetValue<string>();

        // 使用獲得的 token 測試存取
        Environment.SetEnvironmentVariable("VAULT_TOKEN", clientToken);

        if (roleName == "app-dev")
        {
            var readResult = await ExecuteVaultCommandAsync("kv get -format=json dev/db/connection/identity");
            Console.WriteLine($"Read result with {roleName} token:");
            Console.WriteLine(readResult);
        }
        else if (roleName == "app-prod")
        {
            var readResult = await ExecuteVaultCommandAsync("kv get -format=json prod/db/connection/identity");
            Console.WriteLine($"Read result with {roleName} token:");
            Console.WriteLine(readResult);
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
