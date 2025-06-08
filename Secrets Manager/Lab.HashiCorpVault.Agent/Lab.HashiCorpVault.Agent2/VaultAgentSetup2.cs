using System.Diagnostics;
using Lab.HashiCorpVault.Client;

namespace Lab.HashiCorpVault.Agent2;

public class VaultAgentSetup2
{
    private readonly VaultApiClient _vaultApiClient;
    private const string ROLE_ID_FILE = "role_id";
    private const string SECRET_ID_FILE = "secret_id";
    private const string AGENT_CONFIG_FILE = "vault-agent-config.hcl";
    private const string AppRoleName = "app-dev";
    
    public VaultAgentSetup2(VaultApiClient vaultApiClient)
    {
        _vaultApiClient = vaultApiClient;
    }

    public async Task SetupVaultAgentAsync()
    {
        // 取得 Role ID 和 Secret ID（這步驟通常由管理員執行）
        var (roleId, secretId) = await GetAppRoleCredentials(AppRoleName);
        // 將認證信息寫入文件（實際環境中應該通過安全的方式傳遞）
        await SaveCredentialsForVaultAgent(AppRoleName, roleId, secretId);

        // 啟動 Vault Agent
        await StartVaultAgent();
    }

    private async Task<(string roleId, string secretId)> GetAppRoleCredentials(string roleName)
    {
        // 使用 VaultApiClient 取得 Role ID
        var roleResponse = await _vaultApiClient.GetRoleIdAsync(roleName);
        var roleId = roleResponse["data"]?["role_id"]?.GetValue<string>();

        // 使用 VaultApiClient 取得 Secret ID
        var secretResponse = await _vaultApiClient.GenerateSecretIdAsync(roleName);
        var secretId = secretResponse["data"]?["secret_id"]?.GetValue<string>();

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

}
