using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Lab.HashiCorpVault.Agent;

public class VaultAgentSetup
{
    private readonly string _vaultServer;
    private readonly string _rootToken;
    private const string ROLE_ID_FILE = "role_id";
    private const string SECRET_ID_FILE = "secret_id";
    private const string AGENT_CONFIG_FILE = "vault-agent-config.hcl";
    private const string AppRoleName = "app-dev";
    private readonly string _vaultAgentAddress = "http://127.0.0.1:8100/";

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

        // 取得 Role ID 和 Secret ID（這步驟通常由管理員執行）
        var (roleId, secretId) = await GetAppRoleCredentials(AppRoleName);
        // 將認證信息寫入文件（實際環境中應該通過安全的方式傳遞）
        await SaveCredentialsForVaultAgent(AppRoleName, roleId, secretId);

        // 啟動 Vault Agent
        await StartVaultAgent();
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

        await ExecuteVaultCommandAsync("kv put dev/db/connection/identity Account=user Password=1111");
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

    private async Task<(string roleId, string secretId)> GetAppRoleCredentials(string roleName)
    {
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

    private async Task SaveCredentialsForVaultAgent(string roleName, string roleId, string secretId)
    {
        Console.WriteLine("儲存認證資訊供 Vault Agent 使用...");

        // 建立目錄
        var credentialsDir = Path.Combine("vault-credentials", roleName);
        if (!Directory.Exists(credentialsDir))
        {
            Directory.CreateDirectory(credentialsDir);
        }
        credentialsDir = "";
        // 儲存 Role ID 和 Secret ID
        await File.WriteAllTextAsync(Path.Combine(credentialsDir, ROLE_ID_FILE), roleId);
        await File.WriteAllTextAsync(Path.Combine(credentialsDir, SECRET_ID_FILE), secretId);

        Console.WriteLine($"認證資訊已儲存到 {credentialsDir} 目錄");
    }

    private async Task StartVaultAgent()
    {
        Console.WriteLine("啟動 Vault Agent...");

        var startInfo = new ProcessStartInfo
        {
            FileName = "vault",
            Arguments = $"agent -config={AGENT_CONFIG_FILE} -log-level=trace", // 使用 trace 級別
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = false,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };

        using var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data) == false)
            {
                Console.WriteLine($"[AGENT] {e.Data}");

                // 特別關注這些關鍵訊息
                if (e.Data.Contains("template") || e.Data.Contains("rendered") ||
                    e.Data.Contains("error") || e.Data.Contains("failed"))
                {
                    Console.WriteLine($"*** 重要: {e.Data} ***");
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data) == false)
            {
                Console.WriteLine($"[ERROR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // 等待更長時間觀察日誌
        Console.WriteLine("觀察 Vault Agent 日誌 5 秒...");
        await Task.Delay(TimeSpan.FromSeconds(5));

        await VerifyVaultAgentFileAsync();
        await VerifyVaultProcessInfoAsync();
        await VerifyVaultAgentAPI();

        Console.WriteLine("按任意鍵停止...");
        Console.ReadKey();

        if (process.HasExited == false)
        {
            process.Kill();
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

    private async Task VerifyVaultAgentFileAsync()
    {
        Console.WriteLine("驗證 Vault Agent 狀態...");

        // 檢查 PID 文件
        if (File.Exists("vault-agent.pid"))
        {
            var pid = await File.ReadAllTextAsync("vault-agent.pid");
            Console.WriteLine($"Vault Agent PID: {pid.Trim()}");
        }

        // 檢查 token 文件
        if (File.Exists("vault-token"))
        {
            Console.WriteLine("✓ Vault token 文件已生成");
        }

        // 檢查範本輸出
        // var configFiles = new[] { "dev-db-config.json", "prod-db-config.json" };
        var configFiles = new[] { "dev-db-config.json" };
        foreach (var file in configFiles)
        {
            if (File.Exists(file))
            {
                var content = await File.ReadAllTextAsync(file);
                Console.WriteLine($"✓ {file} 已生成:");
                Console.WriteLine(content);
            }
            else
            {
                Console.WriteLine($"✗ {file} 未找到");
            }
        }
    }

    public static async Task VerifyVaultProcessInfoAsync()
    {
        try
        {
            // 檢查進程是否存在
            var processes = Process.GetProcessesByName("vault");
            if (processes.Any())
            {
                Console.WriteLine($"✓ 找到 {processes.Length} 個 vault 進程");
                foreach (var process in processes)
                {
                    Console.WriteLine($"  PID: {process.Id}, 啟動時間: {process.StartTime}");
                }
            }
            else
            {
                Console.WriteLine("✗ 沒有找到 vault 進程");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"檢查進程時發生錯誤: {ex.Message}");
        }
    }

    private async Task VerifyVaultAgentAPI()
    {
        try
        {
            using var client = new HttpClient()
            {
                BaseAddress = new Uri(_vaultAgentAddress)
            };
            var token = await File.ReadAllTextAsync("vault-token");
            client.DefaultRequestHeaders.Add("X-Vault-Token", token.Trim());

            var response = await client.GetAsync("v1/dev/data/db/connection/identity");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✓ Vault Agent API 回應正常");
            }
            else
            {
                Console.WriteLine($"✗ Vault Agent API 回應異常: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 測試 Vault Agent API 時發生錯誤: {ex.Message}");
        }
    }
}
