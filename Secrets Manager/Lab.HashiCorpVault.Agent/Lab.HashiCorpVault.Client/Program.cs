using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lab.HashiCorpVault.Client;

public class AppSettings
{
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();
    public DatabaseCredentials DatabaseCredentials { get; set; } = new();
    public VaultAgentInfo VaultAgent { get; set; } = new();
}

public class VaultAgentInfo
{
    public string LastUpdated { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}

public class Program
{
    static string VaultServer = "http://127.0.0.1:8200";
    static string VaultRootToken = "你的token";

    public static async Task Main(string[] args)
    {
        // 建立 Host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddHttpClient<IVaultAgentService, VaultAgentService>();
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var vaultAgentService = host.Services.GetRequiredService<IVaultAgentService>();

        logger.LogInformation("=== Vault Agent Demo 應用程式啟動 ===");

        try
        {
            // 1. 設定 Vault 和 AppRole
            await SetupVaultEnvironment(logger);

            // 2. 生成 Vault Agent 配置
            await GenerateVaultAgentConfig(logger);

            // 3. 啟動 Vault Agent
            var agentProcess = await StartVaultAgent(logger);

            // 4. 等待 Vault Agent 啟動並測試連線
            await WaitForVaultAgentReady(vaultAgentService, logger);

            // 5. 示範各種存取方式
            await DemonstrateVaultAgentUsage(vaultAgentService, logger);

            // 6. 清理
            logger.LogInformation("按任意鍵停止 Vault Agent 並結束程式...");
            Console.ReadKey();

            if (agentProcess != null && !agentProcess.HasExited)
            {
                agentProcess.Kill();
                agentProcess.Dispose();
                logger.LogInformation("Vault Agent 已停止");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "應用程式執行時發生錯誤");
        }

        logger.LogInformation("=== 應用程式結束 ===");
    }

    private static async Task SetupVaultEnvironment(ILogger logger)
    {
        logger.LogInformation("=== 步驟 1: 設定 Vault 環境 ===");

        var vaultSetup = new VaultAppRoleSetup(VaultServer, VaultRootToken);
        await vaultSetup.SetupVaultAsync();

        logger.LogInformation("Vault 環境設定完成");
    }

    private static async Task GenerateVaultAgentConfig(ILogger logger)
    {
        logger.LogInformation("=== 步驟 2: 生成 Vault Agent 配置 ===");

        var configGenerator = new VaultAgentConfigGenerator(VaultServer);
        await configGenerator.GenerateConfigFilesAsync();
        configGenerator.PrintUsageInstructions();

        logger.LogInformation("Vault Agent 配置生成完成");
    }

    private static async Task<Process?> StartVaultAgent(ILogger logger)
    {
        logger.LogInformation("=== 步驟 3: 啟動 Vault Agent ===");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "vault",
                Arguments = "agent -config=vault-agent.hcl",
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process { StartInfo = startInfo };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logger.LogInformation("Vault Agent: {Output}", e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    logger.LogWarning("Vault Agent Error: {Error}", e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            logger.LogInformation("Vault Agent 已在背景啟動，PID: {ProcessId}", process.Id);
            return process;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "啟動 Vault Agent 失敗");
            return null;
        }
    }

    private static async Task WaitForVaultAgentReady(IVaultAgentService vaultAgentService, ILogger logger)
    {
        logger.LogInformation("=== 步驟 4: 等待 Vault Agent 準備就緒 ===");

        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(2);

        for (int i = 0; i < maxRetries; i++)
        {
            logger.LogInformation("嘗試連接 Vault Agent... ({Attempt}/{MaxRetries})", i + 1, maxRetries);

            if (await vaultAgentService.TestVaultAgentConnectionAsync())
            {
                logger.LogInformation("Vault Agent 連線成功！");
                return;
            }

            if (i < maxRetries - 1)
            {
                logger.LogInformation("等待 {Delay} 秒後重試...", retryDelay.TotalSeconds);
                await Task.Delay(retryDelay);
            }
        }

        throw new Exception("無法連接到 Vault Agent");
    }

    private static async Task DemonstrateVaultAgentUsage(IVaultAgentService vaultAgentService, ILogger logger)
    {
        logger.LogInformation("=== 步驟 5: 示範 Vault Agent 使用方式 ===");

        // 方式 1: 直接透過 HTTP API 存取
        logger.LogInformation("--- 方式 1: 透過 HTTP API 存取 ---");
        var credentials = await vaultAgentService.GetDatabaseCredentialsAsync();
        if (credentials != null)
        {
            logger.LogInformation("成功獲取認證資訊:");
            logger.LogInformation("  使用者名稱: {Username}", credentials.Username);
            logger.LogInformation("  密碼: {Password}", new string('*', credentials.Password.Length));

            var connectionString = await vaultAgentService.GetConnectionStringAsync();
            logger.LogInformation("連接字串已建立");
        }

        // 方式 2: 讀取 Vault Agent 生成的配置檔
        logger.LogInformation("--- 方式 2: 讀取動態生成的配置檔 ---");

        // 等待配置檔生成
        await WaitForConfigFile("./config/appsettings.json", logger);

        var appSettings = await vaultAgentService.ReadConfigFileAsync<AppSettings>("./config/appsettings.json");
        if (appSettings != null)
        {
            logger.LogInformation("成功讀取 appsettings.json:");
            logger.LogInformation("  資料庫使用者: {Username}", appSettings.DatabaseCredentials.Username);
            logger.LogInformation("  連接字串已設定");
            logger.LogInformation("  最後更新時間: {LastUpdated}", appSettings.VaultAgent.LastUpdated);
        }

        // 方式 3: 讀取連接字串配置檔
        logger.LogInformation("--- 方式 3: 讀取連接字串配置檔 ---");

        await WaitForConfigFile("./config/connectionstrings.json", logger);

        var connectionConfig = await vaultAgentService.ReadConfigFileAsync<Dictionary<string, object>>("./config/connectionstrings.json");
        if (connectionConfig != null)
        {
            logger.LogInformation("成功讀取 connectionstrings.json");
            var jsonString = JsonSerializer.Serialize(connectionConfig, new JsonSerializerOptions { WriteIndented = true });
            logger.LogInformation("配置內容:\n{Config}", jsonString);
        }

        // 方式 4: 示範在 ASP.NET Core 中的使用
        logger.LogInformation("--- 方式 4: ASP.NET Core 整合示範 ---");
        await DemonstrateAspNetCoreIntegration(logger);

        // 方式 5: 監控配置檔變更
        logger.LogInformation("--- 方式 5: 監控配置檔變更 ---");
        await DemonstrateConfigFileWatching(logger);
    }

    private static async Task WaitForConfigFile(string filePath, ILogger logger)
    {
        var maxWait = TimeSpan.FromSeconds(30);
        var checkInterval = TimeSpan.FromSeconds(1);
        var startTime = DateTime.Now;

        logger.LogInformation("等待配置檔生成: {FilePath}", filePath);

        while (DateTime.Now - startTime < maxWait)
        {
            if (File.Exists(filePath))
            {
                logger.LogInformation("配置檔已生成: {FilePath}", filePath);
                return;
            }

            await Task.Delay(checkInterval);
        }

        logger.LogWarning("配置檔生成超時: {FilePath}", filePath);
    }

    private static async Task DemonstrateAspNetCoreIntegration(ILogger logger)
    {
        logger.LogInformation("示範 ASP.NET Core 整合方式:");

        var integrationCode = """
                              // 在 ASP.NET Core 應用程式中的使用方式

                              // 1. 在 Program.cs 中設定配置來源
                              var builder = WebApplication.CreateBuilder(args);

                              // 添加 Vault Agent 生成的配置檔
                              builder.Configuration.AddJsonFile("config/appsettings.json", optional: true, reloadOnChange: true);

                              // 2. 設定服務
                              builder.Services.Configure<DatabaseCredentials>(
                                  builder.Configuration.GetSection("DatabaseCredentials"));

                              // 3. 在控制器或服務中使用
                              public class DataController : ControllerBase
                              {
                                  private readonly IOptions<DatabaseCredentials> _dbCredentials;
                                  
                                  public DataController(IOptions<DatabaseCredentials> dbCredentials)
                                  {
                                      _dbCredentials = dbCredentials;
                                  }
                                  
                                  [HttpGet]
                                  public async Task<IActionResult> GetData()
                                  {
                                      var username = _dbCredentials.Value.Username;
                                      var password = _dbCredentials.Value.Password;
                                      
                                      // 使用認證資訊連接資料庫
                                      // ...
                                      
                                      return Ok();
                                  }
                              }
                              """;

        logger.LogInformation("ASP.NET Core 整合程式碼範例:\n{Code}", integrationCode);
    }

    private static async Task DemonstrateConfigFileWatching(ILogger logger)
    {
        logger.LogInformation("啟動配置檔監控 (10 秒)...");

        var configPath = "./config/appsettings.json";
        if (!File.Exists(configPath))
        {
            logger.LogWarning("配置檔不存在，跳過監控示範");
            return;
        }

        using var watcher = new FileSystemWatcher(Path.GetDirectoryName(configPath)!, Path.GetFileName(configPath));

        var lastModified = DateTime.MinValue;

        watcher.Changed += (sender, e) =>
        {
            // 避免重複觸發
            if (DateTime.Now - lastModified < TimeSpan.FromSeconds(1))
                return;

            lastModified = DateTime.Now;
            logger.LogInformation("配置檔已更新: {FileName} at {Time}", e.Name, DateTime.Now);

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500); // 等待檔案寫入完成
                    var content = await File.ReadAllTextAsync(configPath);
                    var config = JsonSerializer.Deserialize<AppSettings>(content);
                    logger.LogInformation("新的配置已載入，最後更新: {LastUpdated}",
                        config?.VaultAgent?.LastUpdated ?? "未知");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "讀取更新的配置檔時發生錯誤");
                }
            });
        };

        watcher.EnableRaisingEvents = true;

        logger.LogInformation("配置檔監控已啟動，監控路徑: {Path}", configPath);
        await Task.Delay(TimeSpan.FromSeconds(10));

        watcher.EnableRaisingEvents = false;
        logger.LogInformation("配置檔監控已停止");
    }
}
