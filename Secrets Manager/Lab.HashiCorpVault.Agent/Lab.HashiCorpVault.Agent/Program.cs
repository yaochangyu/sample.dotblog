namespace Lab.HashiCorpVault.Agent;

class Program
{
    private static string VaultRootToken = "你的token";
    private static string VaultServer = "http://127.0.0.1:8200";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var setup = new VaultAgentSetup(VaultServer, VaultRootToken);
        await setup.SetupVaultAgentAsync();
        Console.WriteLine("服務已啟動，請稍後檢查 Vault Agent 狀態...");

        // 驗證 Agent 狀態
        await CheckRequiredFiles();
    }

    private static async Task CheckRequiredFiles()
    {
        var requiredFiles = new[]
        {
            "role_id",
            "secret_id",
            "vault-agent-config.hcl",
            "dev-db-config.ctmpl",
            "prod-db-config.ctmpl"
        };

        foreach (var file in requiredFiles)
        {
            if (File.Exists(file))
            {
                Console.WriteLine($"✓ {file} 存在");
                // 顯示文件內容（除了敏感信息）
                if (file.EndsWith(".ctmpl") || file.EndsWith(".hcl"))
                {
                    var content = await File.ReadAllTextAsync(file);
                    Console.WriteLine($"  內容預覽: {content.Substring(0, Math.Min(100, content.Length))}...");
                }
            }
            else
            {
                Console.WriteLine($"✗ {file} 不存在");
            }
        }
    }
}
