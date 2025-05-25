using System.Diagnostics;
using System.Text.Json;

namespace Lab.HashiCorpVault.Test;

public class VaultSetup
{
    private string _vaultServer;
    private string _rootToken;
    public VaultSetup(string vaultServer, string rootToken)
    {
        _vaultServer = vaultServer;
        _rootToken = rootToken;
    }

    private readonly Dictionary<string, string> _policies = new()
    {
        ["team-1"] = """
                     #path "secret/*" {
                     #  capabilities = ["read","list"]
                     #}

                     path "secret/db/*" {
                       capabilities = ["create", "read", "update", "delete", "list"]
                     }

                     #path "secret/data/db/connection" {
                     #  capabilities = ["read"]
                     #}

                     #
                     #path "secret/data/db/connection/data" {
                     #  capabilities = ["read"]
                     #}
                     """,
        ["team-2"] = """
                     path "secret/data/db/connection" {
                       capabilities = ["read"]
                     }

                     path "secret/data/db/connection/data" {
                       capabilities = ["read"]
                     }
                     """,
        ["team-3"] = """
                     path "secret/data/db/connection" {
                       capabilities = ["read"]
                     }

                     path "secret/data/db/connection/data" {
                       capabilities = ["read"]
                     }
                     """,
        ["team-admin"] = """
                         path "secret/data/db/connection" {
                           capabilities = ["create", "read", "update", "delete", "list"]
                         }

                         path "secret/data/db/connection/data" {
                           capabilities = ["create", "read", "update", "delete", "list"]
                         }
                         """
    };

    private readonly List<(string Name, string Policy)> _tokens = new()
    {
        ("team-1", "team-1"),
        ("team-2", "team-2"),
        ("team-3", "team-3"),
        ("team-admin", "team-admin")
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
            await ExecuteVaultCommandAsync("secrets enable -path=secret kv-v2");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        // 建立 Policies
        Console.WriteLine("Creating policies...");
        foreach (var (policyName, policyContent) in _policies)
        {
            string policyFile = $"{policyName}-policy.hcl";
            await File.WriteAllTextAsync(policyFile, policyContent);
            await ExecuteVaultCommandAsync($"policy write {policyName} {policyFile}");
            File.Delete(policyFile);
        }

        // 建立 Secrets
        Console.WriteLine("Writing secrets to kv v2...");
        await ExecuteVaultCommandAsync("kv put secret/db/connection key-1=123456 key-2=77889 key-3=0806449");

        // 建立 Tokens
        Console.WriteLine("Creating tokens...");
        foreach (var (tokenName, policyName) in _tokens)
        {
            var result = await ExecuteVaultCommandAsync($"token create -policy={policyName} -format=json");
            var tokenData = JsonDocument.Parse(result);
            var clientToken = tokenData.RootElement.GetProperty("auth").GetProperty("client_token").GetString();
            await File.WriteAllTextAsync($"{tokenName}-token.txt", clientToken);
            Console.WriteLine($"{tokenName} token created and saved to {tokenName}-token.txt");
        }

        // 使用 team-1 token 測試讀取秘密
        try
        {
            Console.WriteLine("Testing read access with team-1 token...");
            var teamToken = await File.ReadAllTextAsync("team-1-token.txt");
            Environment.SetEnvironmentVariable("VAULT_TOKEN", teamToken);
            var readResult = await ExecuteVaultCommandAsync("kv get -format=json secret/db/connection");
            Console.WriteLine("Read result with team-1 token:");
            Console.WriteLine(readResult);

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }


        // 使用 team-1 token 測試讀取秘密
        try
        {
            Console.WriteLine("Testing read access with team-1 token...");
            var teamToken = await File.ReadAllTextAsync("team-1-token.txt");
            Environment.SetEnvironmentVariable("VAULT_TOKEN", teamToken);
            var readResult = await ExecuteVaultCommandAsync("kv get -mount=secret db/connection");
            Console.WriteLine("Read result with team-1 token:");
            Console.WriteLine(readResult);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
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
