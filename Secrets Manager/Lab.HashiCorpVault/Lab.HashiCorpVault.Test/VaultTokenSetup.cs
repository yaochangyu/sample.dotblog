using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lab.HashiCorpVault.Test;

public class VaultTokenSetup
{
    private readonly string _vaultServer;
    private readonly string _vaultToken;
    public VaultTokenSetup(string vaultServer, string vaultToken)
    {
        _vaultServer = vaultServer;
        _vaultToken = vaultToken;
    }

    private readonly Dictionary<string, string> _policies = new()
    {
        ["team-dev"] = """
                       path "dev/*" {
                         capabilities = ["list"]
                       }

                       path "dev/data/db/connection/*" {
                         capabilities = ["read","list"]
                       }
                       """,
        ["team-prod"] = """
                        path "prod/data/db/connection/*" {
                          capabilities = ["read","list"]
                        }
                        """,
        //         ["team-1"] = """
        //                      #path "secret/*" {
        //                      #  capabilities = ["read","list"]
        //                      #}
        //
        //                      #path "secret/db/*" {
        //                      #  capabilities = ["create", "read", "update", "delete", "list"]
        //                      #}
        //
        //                      path "secret/data/db/connection" {
        //                        capabilities = ["read"]
        //                      }
        //
        //
        //                      path "secret/data/db/connection/data" {
        //                        capabilities = ["read"]
        //                      }
        //                      """,
        //         ["team-2"] = """
        //                      path "secret/data/db/connection" {
        //                        capabilities = ["read"]
        //                      }
        //
        //                      path "secret/data/db/connection/data" {
        //                        capabilities = ["read"]
        //                      }
        //                      """,
        //         ["team-3"] = """
        //                      path "secret/data/db/connection" {
        //                        capabilities = ["read"]
        //                      }
        //
        //                      path "secret/data/db/connection/data" {
        //                        capabilities = ["read"]
        //                      }
        //                      """,
        //         ["team-admin"] = """
        //                          path "secret/data/db/connection" {
        //                            capabilities = ["create", "read", "update", "delete", "list"]
        //                          }
        //
        //                          path "secret/data/db/connection/data" {
        //                            capabilities = ["create", "read", "update", "delete", "list"]
        //                          }
        //                          """
    };

    public async Task SetupVaultAsync()
    {
        // 設定環境變數
        Environment.SetEnvironmentVariable("VAULT_ADDR", _vaultServer);
        Environment.SetEnvironmentVariable("VAULT_TOKEN", _vaultToken);

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
        await ExecuteVaultCommandAsync("kv put secret/db/connection key-1=123456 key-2=77889 key-3=0806449");

        await ExecuteVaultCommandAsync("kv put dev/db/connection/identity Account=user Password=1111");
        await ExecuteVaultCommandAsync("kv put prod/db/connection/identity Account=user Password=111111");

        // 建立 Policies
        Console.WriteLine("Creating policies...");
        foreach (var (policyName, policyContent) in _policies)
        {
            string policyFile = $"{policyName}-policy.hcl";
            await File.WriteAllTextAsync(policyFile, policyContent);
            await ExecuteVaultCommandAsync($"policy write {policyName} {policyFile}");
            File.Delete(policyFile);
        }

        // 建立 Tokens
        Console.WriteLine("Creating Tokens...");
        foreach (var (policyName, _) in _policies)
        {
            // 建立 Token
            // child token 的 ttl 跟著 root token 設定(Max Lease TTL)
            var result = await ExecuteVaultCommandAsync($"token create -policy={policyName} -format=json -ttl=360d");
            /*
            列舉目前的 token：vault list auth/token/accessors
            查看特定 token：vault token lookup -accessor <accessor-id>
            */
            var tokenJsonObject = JsonNode.Parse(result).AsObject();
            var token = tokenJsonObject["auth"]?["client_token"]?.GetValue<string>();

            await File.WriteAllTextAsync($"{policyName}-token.txt", token);
            Console.WriteLine($"{policyName} token created and saved to {policyName}-token.txt");
        }

        // 測試使用 Token 登入，取得機敏性資料
        Console.WriteLine("Read Secret Data...");
        foreach (var (policyName, _) in _policies)
        {
            await LoginTokenAsync(policyName);
        }
    }

    private async Task LoginTokenAsync(string tokenName)
    {
        Console.WriteLine($"\nTesting AppRole login for {tokenName}...");

        // 讀取 Token
        var clientToken = await File.ReadAllTextAsync($"{tokenName}-token.txt");

        // 使用獲得的 Token 測試存取
        Environment.SetEnvironmentVariable("VAULT_TOKEN", clientToken);

        if (tokenName == "team-dev")
        {
            var readResult = await ExecuteVaultCommandAsync("kv get -format=json dev/db/connection/identity");
            Console.WriteLine($"Read result with {tokenName} token:");
            Console.WriteLine(readResult);
        }
        else if (tokenName == "team-prod")
        {
            var readResult = await ExecuteVaultCommandAsync("kv get -mount=prod db/connection/identity");
            Console.WriteLine($"Read result with {tokenName} token:");
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
