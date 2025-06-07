using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Lab.HashiCorpVault.ConfigGenerator;

public class VaultAppRoleSetup
{
    private readonly string _vaultServer;
    private readonly string _rootToken;

    public VaultAppRoleSetup(string vaultServer, string rootToken)
    {
        _vaultServer = vaultServer;
        _rootToken = rootToken;
    }

    private readonly Dictionary<string, string> _clientPolicies = new()
    {
        ["app-dev"] = """
                      # 允許讀取 app-dev 相關的機密資料
                      path "secret/data/app-dev/*" {
                        capabilities = ["read"]
                      }

                      path "secret/metadata/app-dev/*" {
                        capabilities = ["read"]
                      }

                      # 允許讀取自己的 role-id（用於 Vault Agent）
                      path "auth/approle/role/app-dev/role-id" {
                        capabilities = ["read"]
                      }
                      """
    };

    public async Task SetupVaultAsync()
    {
        // 設定環境變數
        Environment.SetEnvironmentVariable("VAULT_ADDR", _vaultServer);
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _rootToken);

        Console.WriteLine("=== 開始設定 Vault AppRole for app-dev ===");

        // 執行管理者設定
        await AdminSetup();

        // 建立機密資料
        await CreateSecretData();

        // 建立 AppRole 並獲取認證資訊
        var (roleId, secretId) = await CreateAppRoleCredentials();

        // 儲存認證資訊到檔案（供 Vault Agent 使用）
        await SaveCredentialsForVaultAgent(roleId, secretId);

        // 測試 AppRole 登入
        await TestAppRoleLogin(roleId, secretId);

        Console.WriteLine("=== Vault 設定完成 ===");
    }

    private async Task AdminSetup()
    {
        try
        {
            Console.WriteLine("啟用 KV v2 秘密引擎...");
            await ExecuteVaultCommandAsync("secrets enable -path=secret kv-v2");
        }
        catch (Exception e)
        {
            Console.WriteLine($"KV v2 可能已經啟用: {e.Message}");
        }

        try
        {
            Console.WriteLine("啟用 AppRole 認證方法...");
            await ExecuteVaultCommandAsync("auth enable approle");
        }
        catch (Exception e)
        {
            Console.WriteLine($"AppRole 可能已經啟用: {e.Message}");
        }

        // 建立政策
        Console.WriteLine("建立 app-dev 政策...");
        foreach (var (policyName, policyContent) in _clientPolicies)
        {
            string policyFile = $"{policyName}-policy.hcl";
            await File.WriteAllTextAsync(policyFile, policyContent);
            await ExecuteVaultCommandAsync($"policy write {policyName} {policyFile}");
            File.Delete(policyFile);
            Console.WriteLine($"政策 {policyName} 建立完成");
        }

        // 建立 AppRole
        Console.WriteLine("建立 app-dev AppRole...");
        await ExecuteVaultCommandAsync("write auth/approle/role/app-dev " +
            "token_policies=app-dev " +
            "token_ttl=1h " +
            "token_max_ttl=4h " +
            "bind_secret_id=true");
    }

    private async Task CreateSecretData()
    {
        Console.WriteLine("建立機密資料...");
        
        // 根據需求建立機密資料：account 和 password
        await ExecuteVaultCommandAsync("kv put secret/app-dev/database " +
            "account=example_user " +
            "password=example_password");

        Console.WriteLine("機密資料建立完成：");
        Console.WriteLine("  - account: example_user");
        Console.WriteLine("  - password: example_password");
    }

    private async Task<(string roleId, string secretId)> CreateAppRoleCredentials()
    {
        Console.WriteLine("獲取 AppRole 認證資訊...");

        // 取得 Role ID
        var roleResult = await ExecuteVaultCommandAsync("read -format=json auth/approle/role/app-dev/role-id");
        var roleJsonObject = JsonNode.Parse(roleResult).AsObject();
        var roleId = roleJsonObject["data"]?["role_id"]?.GetValue<string>();

        // 取得 Secret ID
        var secretResult = await ExecuteVaultCommandAsync("write -format=json -f auth/approle/role/app-dev/secret-id");
        var secretJsonObject = JsonNode.Parse(secretResult).AsObject();
        var secretId = secretJsonObject["data"]?["secret_id"]?.GetValue<string>();

        Console.WriteLine($"Role ID: {roleId}");
        Console.WriteLine($"Secret ID: {secretId}");

        return (roleId, secretId);
    }

    private async Task SaveCredentialsForVaultAgent(string roleId, string secretId)
    {
        Console.WriteLine("儲存認證資訊供 Vault Agent 使用...");

        // 建立目錄
        var credentialsDir = "vault-credentials";
        if (!Directory.Exists(credentialsDir))
        {
            Directory.CreateDirectory(credentialsDir);
        }

        // 儲存 Role ID 和 Secret ID
        await File.WriteAllTextAsync(Path.Combine(credentialsDir, "role-id"), roleId);
        await File.WriteAllTextAsync(Path.Combine(credentialsDir, "secret-id"), secretId);

        Console.WriteLine($"認證資訊已儲存到 {credentialsDir} 目錄");
    }

    private async Task TestAppRoleLogin(string roleId, string secretId)
    {
        Console.WriteLine("測試 AppRole 登入...");

        // 使用 AppRole 登入
        var tokenResult = await ExecuteVaultCommandAsync($"write -format=json auth/approle/login role_id={roleId} secret_id={secretId}");
        var tokenJsonObject = JsonNode.Parse(tokenResult).AsObject();
        var clientToken = tokenJsonObject["auth"]?["client_token"]?.GetValue<string>();

        // 使用獲得的 token 測試存取
        Environment.SetEnvironmentVariable("VAULT_TOKEN", clientToken);

        var readResult = await ExecuteVaultCommandAsync("kv get -format=json secret/app-dev/database");
        Console.WriteLine("成功讀取機密資料:");
        Console.WriteLine(readResult);

        // 恢復 root token
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _rootToken);
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