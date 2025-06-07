using System.Diagnostics;

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
    
    private readonly Dictionary<string, string> _clientPolicies = new()
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
    }

    public async Task SetupVaultAgentAsync()
    {
        // 設定環境變數
        Environment.SetEnvironmentVariable("VAULT_ADDR", _vaultServer);
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _rootToken);

        // 建立 AppRole 認證方法
        await SetupAppRoleAsync();

        // 建立機敏性資料
        await SetupSecretAsync();
    }

    async Task SetupSecretAsync()
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

    private async Task SetupAppRoleAsync()
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
