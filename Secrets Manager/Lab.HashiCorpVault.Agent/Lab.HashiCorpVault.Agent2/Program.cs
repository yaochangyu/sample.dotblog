using System.Text.Json;
using System.Text.Json.Nodes;
using Lab.HashiCorpVault.Client;

namespace Lab.HashiCorpVault.Agent2;

class Program
{
    private static string VaultServer = "http://127.0.0.1:8200/";
    private static string VaultToken = "你的 token";
    private static string VaultAgentAddress = "http://127.0.0.1:8100/";
    private static FileSystemWatcher watcher = null;
    private static string VaultFileName = "vault-token";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Vault Agent Starting...");

        Console.Write("Please enter your Vault token: ");
        string vaultToken = Console.ReadLine() ?? VaultToken;

        if (string.IsNullOrWhiteSpace(vaultToken))
        {
            Console.WriteLine("Error: Vault token cannot be empty.");
            return;
        }

        // 讀取 .vault-token 檔案，取得 root token
        var rootVaultFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".vault-token");
        string vaultRootToken = string.Empty;

        if (File.Exists(rootVaultFilePath) == false)
        {
            Console.Write("Please enter your Vault root token: ");
            vaultRootToken = Console.ReadLine();
        }
        else
        {
            vaultRootToken = await File.ReadAllTextAsync(rootVaultFilePath);
            Console.WriteLine($"讀取 Vault root token: {vaultRootToken}");
        }
        
        if (string.IsNullOrWhiteSpace(vaultRootToken))
        {
            Console.WriteLine("Error: Vault root token cannot be empty.");
            return;
        }
        
        // 應該要有兩個 Process 
        await VaultAgentSetup2.VerifyVaultProcessInfoAsync();

        // 設定 Vault Agent
        var setup = new VaultAgentSetup2(new VaultApiClient(new HttpClient
        {
            BaseAddress = new Uri(VaultServer),
        }, vaultToken));

        await setup.SetupVaultAgentAsync();
        // await setup.StartVaultAgentAsync();// Agent 無法單獨啟動，必須要要走完整個流程，關閉後就無法正常工作

        Console.WriteLine("Vault Agent Started.");

        Console.WriteLine("透過 Vault Agent 讀取秘密...");
        var agentToken = await File.ReadAllTextAsync(VaultFileName);
        var vaultApiClient = new VaultApiClient(new HttpClient
        {
            BaseAddress = new Uri(VaultAgentAddress),

        }, agentToken);
        Console.WriteLine($"使用 Vault Agent Token: {agentToken}");

        await PrintSecretDataAsync(vaultApiClient);
        SetupTokenWatcher();
        // 監控 token 文件變化
        var tokenFileInfo = new FileInfo(VaultFileName);
        var lastTokenTimeUtc = tokenFileInfo.LastWriteTimeUtc;

        while (true)
        {
            try
            {
                // 顯示選單並等待用戶輸入
                Console.WriteLine();
                Console.WriteLine("請選擇操作：");
                Console.WriteLine("1. 立即讀取秘密");
                Console.WriteLine("2. 顯示當前 Token 資訊");
                Console.WriteLine("3. 退出程序");

                var key = Console.ReadKey(true);
                Console.WriteLine();

                switch (key.KeyChar)
                {
                    case '1':
                        // 檢查 token 文件的最後修改時間
                        tokenFileInfo = new FileInfo(VaultFileName);
                        if (tokenFileInfo.LastWriteTimeUtc > lastTokenTimeUtc)
                        {
                            lastTokenTimeUtc = tokenFileInfo.LastWriteTimeUtc;
                            Console.WriteLine($"[{DateTime.UtcNow}] 檢測到 Token 更新");
                            Console.WriteLine($"Token 文件更新時間: {lastTokenTimeUtc}");

                            // 讀取並使用新的 token
                            agentToken = await File.ReadAllTextAsync(VaultFileName);
                            Console.WriteLine($"成功獲取新的 Token：{agentToken}");
                            vaultApiClient.UpdateToken(agentToken);
                            Console.WriteLine($"使用 Vault Agent Token: {agentToken}");
                        }

                        await PrintSecretDataAsync(vaultApiClient);
                        break;

                    case '2':
                        var currentToken = await File.ReadAllTextAsync(VaultFileName);
                        var currentFileInfo = new FileInfo(VaultFileName);

                        Console.WriteLine($"當前 Token: {currentToken}");
                        Console.WriteLine($"Token 文件最後更新時間: {currentFileInfo.LastWriteTimeUtc}");

                        // 查看 Token 的有效期
                        vaultApiClient.UpdateToken(vaultRootToken);
                        var tokenInfo = await vaultApiClient.GetTokenInfoAsync(currentToken);
                        var tokenInfoString = JsonSerializer.Serialize(tokenInfo);
                        Console.WriteLine("Token Info：");
                        Console.WriteLine($"{tokenInfoString}");
                        vaultApiClient.UpdateToken(agentToken);
                        break;

                    case '3':
                        Console.WriteLine("程序退出中...");
                        if (watcher != null)
                        {
                            watcher.Dispose();
                        }

                        return;

                    default:
                        Console.WriteLine("無效的選擇，請重新輸入");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n發生錯誤: {ex.Message}");
            }
        }
    }
    private static async Task<bool> PrintSecretDataAsync(VaultApiClient vaultApiClient)
    {

        var secretResult = await vaultApiClient.GetSecretAsync("dev/data/db/connection/identity");
        var secretData = secretResult["data"]?["data"] as JsonObject;
        if (secretData == null)
        {
            Console.WriteLine("Error: Unable to read secret data.");
            return true;
        }
        if (secretData.ContainsKey("Account") && secretData.ContainsKey("Password"))
        {
            Console.WriteLine($"當前秘密值：");
            Console.WriteLine($"Account: {secretData["Account"]}");
            Console.WriteLine($"Password: {secretData["Password"]}");
            Console.WriteLine("讀取秘密成功");
        }
        else
        {
            Console.WriteLine("Error: Account or Password not found in secret data.");
        }
        return false;
    }

    private static void SetupTokenWatcher()
    {
        watcher = new FileSystemWatcher
        {
            Path = ".",
            Filter = "vault-token",
            NotifyFilter = NotifyFilters.LastWrite
        };

        watcher.Changed += async (sender, e) =>
        {
            try
            {
                // 為了確保檔案完全寫入，稍微延遲一下
                await Task.Delay(100);

                var newToken = await File.ReadAllTextAsync("vault-token");
                Console.WriteLine($"\n[{DateTime.UtcNow}] 檢測到 Token 更新");
                Console.WriteLine($"成功獲取新的 Token：{newToken}");

                // 產生新的檔案，代表有捕捉到 vault-token 更新
                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
                string newTokenFileName = $"vault-token-updated-{timestamp}";
                await File.WriteAllTextAsync(newTokenFileName, newToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n監聽 Token 更新時發生錯誤: {ex.Message}");
            }
        };

        watcher.EnableRaisingEvents = true;
    }
}
