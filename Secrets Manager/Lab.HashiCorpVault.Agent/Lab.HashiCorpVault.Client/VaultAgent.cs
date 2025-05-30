using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Lab.HashiCorpVault.Client;

public class DatabaseCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class VaultResponse<T>
{
    public T Data { get; set; } = default(T)!;
}

public class VaultSecretData
{
    public Dictionary<string, string> Data { get; set; } = new();
}

public interface IVaultAgentService
{
    Task<DatabaseCredentials?> GetDatabaseCredentialsAsync();
    Task<string?> GetConnectionStringAsync();
    Task<bool> TestVaultAgentConnectionAsync();
    Task<T?> ReadConfigFileAsync<T>(string filePath) where T : class;
}

public class VaultAgentService : IVaultAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VaultAgentService> _logger;
    private readonly string _vaultAgentAddress;

    public VaultAgentService(HttpClient httpClient, ILogger<VaultAgentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _vaultAgentAddress = "http://127.0.0.1:8100";
    }

    public async Task<bool> TestVaultAgentConnectionAsync()
    {
        try
        {
            _logger.LogInformation("測試 Vault Agent 連線...");
            
            // 測試 Vault Agent 健康狀態
            var response = await _httpClient.GetAsync($"{_vaultAgentAddress}/v1/sys/health");
            var isHealthy = response.IsSuccessStatusCode;
            
            _logger.LogInformation("Vault Agent 連線測試: {Status}", isHealthy ? "成功" : "失敗");
            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vault Agent 連線測試失敗");
            return false;
        }
    }

    public async Task<DatabaseCredentials?> GetDatabaseCredentialsAsync()
    {
        try
        {
            _logger.LogInformation("從 Vault Agent 獲取資料庫認證...");

            // 透過 Vault Agent 代理存取秘密
            var response = await _httpClient.GetAsync($"{_vaultAgentAddress}/v1/secret/data/app-dev/database");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("無法從 Vault Agent 獲取秘密資料. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var vaultResponse = JsonSerializer.Deserialize<VaultResponse<VaultSecretData>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (vaultResponse?.Data?.Data != null)
            {
                var credentials = new DatabaseCredentials
                {
                    Username = vaultResponse.Data.Data.GetValueOrDefault("account", ""),
                    Password = vaultResponse.Data.Data.GetValueOrDefault("password", "")
                };

                _logger.LogInformation("成功獲取資料庫認證資訊，使用者: {Username}", credentials.Username);
                return credentials;
            }

            _logger.LogWarning("Vault Agent 回應格式不正確");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "獲取資料庫認證時發生錯誤");
            return null;
        }
    }

    public async Task<string?> GetConnectionStringAsync()
    {
        try
        {
            var credentials = await GetDatabaseCredentialsAsync();
            if (credentials != null)
            {
                var connectionString = $"Server=localhost;Database=MyApp;User Id={credentials.Username};Password={credentials.Password};TrustServerCertificate=true;";
                _logger.LogInformation("成功建立連接字串");
                return connectionString;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "建立連接字串時發生錯誤");
            return null;
        }
    }

    public async Task<T?> ReadConfigFileAsync<T>(string filePath) where T : class
    {
        try
        {
            _logger.LogInformation("讀取配置文件: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("配置文件不存在: {FilePath}", filePath);
                return null;
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("成功讀取配置文件");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取配置文件時發生錯誤: {FilePath}", filePath);
            return null;
        }
    }
}