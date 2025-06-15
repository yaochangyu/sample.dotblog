using System.Text.Json.Nodes;
using Lab.HashiCorpVault.Client;

namespace Lab.HashiCorpVault.Agent2;

class Program
{
    private static string VaultServer = "http://127.0.0.1:8200/";
    private static string VaultToken = "你的 token";
    private static string VaultAgentAddress = "http://127.0.0.1:8100/";

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

        // 設定 Vault Agent
        var setup = new VaultAgentSetup2(new VaultApiClient(new HttpClient
        {
            BaseAddress = new Uri(VaultServer),
        }, vaultToken));

        await setup.SetupVaultAgentAsync();
        Console.WriteLine("Vault Agent Started.");

        Console.WriteLine("透過 Vault Agent 讀取秘密...");
        var agentToken = await File.ReadAllTextAsync("vault-token");
        var vaultApiClient = new VaultApiClient(new HttpClient
        {
            BaseAddress = new Uri(VaultAgentAddress),

        }, agentToken);
        Console.WriteLine($"使用 Vault Agent Token: {agentToken}");

        await PrintSecretDataAsync(vaultApiClient);

        // 監控 token 文件變化
        var tokenFileInfo = new FileInfo("vault-token");
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
                        tokenFileInfo = new FileInfo("vault-token");
                        if (tokenFileInfo.LastWriteTimeUtc > lastTokenTimeUtc)
                        {
                            lastTokenTimeUtc = tokenFileInfo.LastWriteTimeUtc;
                            Console.WriteLine($"[{DateTime.UtcNow}] 檢測到 Token 更新");
                            Console.WriteLine($"Token 文件更新時間: {lastTokenTimeUtc}");

                            // 讀取並使用新的 token
                            agentToken = await File.ReadAllTextAsync("vault-token");
                            Console.WriteLine($"成功獲取新的 Token：{agentToken}");
                            vaultApiClient.UpdateToken(agentToken);
                            Console.WriteLine($"使用 Vault Agent Token: {agentToken}");
                        }

                        await PrintSecretDataAsync(vaultApiClient);
                        break;

                    case '2':
                        var currentToken = await File.ReadAllTextAsync("vault-token");
                        var currentFileInfo = new FileInfo("vault-token");

                        Console.WriteLine($"當前 Token: {currentToken}");
                        Console.WriteLine($"Token 文件最後更新時間: {currentFileInfo.LastWriteTimeUtc}");
                        break;

                    case '3':
                        Console.WriteLine("程序退出中...");
                        return;

                    default:
                        Console.WriteLine("無效的選擇，請重新輸入");
                        break;
                }

                // 短暫休眠以降低 CPU 使用率
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n發生錯誤: {ex.Message}");
                await Task.Delay(5000); // 發生錯誤時等待較長時間
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
}
