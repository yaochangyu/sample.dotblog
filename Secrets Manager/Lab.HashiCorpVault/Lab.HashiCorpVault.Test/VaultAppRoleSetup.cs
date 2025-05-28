using System.Diagnostics;
using System.Text.Json;

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

    private readonly Dictionary<string, string> _policies = new()
    {
        // ["app-dev"] = """
        //               path "dev/data/db/connection/*" {
        //                 capabilities = ["read"]
        //               }
        //               """,
        ["app-prod"] = """
                       path "prod/data/db/connection/*" {
                         capabilities = ["read"]
                       }
                       """
    };

    public async Task SetupVaultAsync()
    {
        // 設定環境變數
        Environment.SetEnvironmentVariable("VAULT_ADDR", _vaultServer);
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _rootToken);

        // 啟用 kv v2 秘密引擎
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

        // 建立 Secrets
        Console.WriteLine("Writing secrets to kv v2...");

        await ExecuteVaultCommandAsync("kv put dev/db/connection/identity Account=user Password=1111");
        await ExecuteVaultCommandAsync("kv put prod/db/connection/identity Account=user Password=111111");

        try
        {
            // 啟用 AppRole 認證方式
            Console.WriteLine("Enabling AppRole auth method...");
            await ExecuteVaultCommandAsync("auth enable approle");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        // 建立 Policies
        Console.WriteLine("Creating AppRole policies...");
        foreach (var (policyName, policyContent) in _policies)
        {
            string policyFile = $"{policyName}-policy.hcl";
            await File.WriteAllTextAsync(policyFile, policyContent);
            await ExecuteVaultCommandAsync($"policy write {policyName} {policyFile}");
            File.Delete(policyFile);
        }

        // 建立 AppRoles
        Console.WriteLine("Creating AppRoles...");
        foreach (var (roleName, _) in _policies)
        {
            // 建立 AppRole
            await ExecuteVaultCommandAsync($"write auth/approle/role/{roleName} token_policies={roleName} token_ttl=1h token_max_ttl=4h");

            // 取得 Role ID
            var roleIdResult = await ExecuteVaultCommandAsync($"read -format=json auth/approle/role/{roleName}/role-id");
            var roleIdJson = JsonDocument.Parse(roleIdResult);
            var roleId = roleIdJson.RootElement.GetProperty("data").GetProperty("role_id").GetString();
            await File.WriteAllTextAsync($"{roleName}-role-id.txt", roleId);

            // 產生 Secret ID
            var secretIdResult = await ExecuteVaultCommandAsync($"write -format=json -f auth/approle/role/{roleName}/secret-id");
            var secretIdJson = JsonDocument.Parse(secretIdResult);
            var secretId = secretIdJson.RootElement.GetProperty("data").GetProperty("secret_id").GetString();
            await File.WriteAllTextAsync($"{roleName}-secret-id.txt", secretId);

            Console.WriteLine($"AppRole {roleName} created. Role ID and Secret ID saved to files.");

            // 測試使用 AppRole 登入
            await AppRoleLogin(roleName);
        }
    }

    private async Task AppRoleLogin(string roleName)
    {
        try
        {
            Console.WriteLine($"\nTesting AppRole login for {roleName}...");

            // 讀取 Role ID 和 Secret ID
            var roleId = await File.ReadAllTextAsync($"{roleName}-role-id.txt");
            var secretId = await File.ReadAllTextAsync($"{roleName}-secret-id.txt");

            // 使用 AppRole 登入
            var loginResult = await ExecuteVaultCommandAsync($"write -format=json auth/approle/login role_id={roleId} secret_id={secretId}");

            var loginJson = JsonDocument.Parse(loginResult);
            var clientToken = loginJson.RootElement.GetProperty("auth").GetProperty("client_token").GetString();

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
        catch (Exception e)
        {
            Console.WriteLine($"Error testing AppRole {roleName}: {e.Message}");
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
