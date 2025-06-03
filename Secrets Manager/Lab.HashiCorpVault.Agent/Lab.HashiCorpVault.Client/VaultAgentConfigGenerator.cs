using System.Text;

namespace Lab.HashiCorpVault.Client;

public class VaultAgentConfigGenerator
{
    private readonly string _vaultAddress;
    private readonly string _roleIdPath;
    private readonly string _secretIdPath;

    public VaultAgentConfigGenerator(string vaultAddress)
    {
        _vaultAddress = vaultAddress;
        _roleIdPath = "./vault-credentials/role-id";
        _secretIdPath = "./vault-credentials/secret-id";
    }

    public async Task GenerateConfigFilesAsync()
    {
        Console.WriteLine("生成 Vault Agent 配置文件...");

        // 建立必要的目錄
        CreateDirectories();

        // 生成主配置文件
        await GenerateMainConfigAsync();

        // 生成模板文件
        await GenerateTemplatesAsync();

        Console.WriteLine("Vault Agent 配置文件生成完成");
    }

    private void CreateDirectories()
    {
        var directories = new[] { "templates", "config", "vault-credentials" };
        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Console.WriteLine($"建立目錄: {dir}");
            }
        }
    }

    private async Task GenerateMainConfigAsync()
    {
        var config = new StringBuilder();
        config.AppendLine("# Vault Agent 配置文件");
        config.AppendLine("# 自動生成 - 請勿手動修改");
        config.AppendLine();
        
        config.AppendLine("# Vault 服務器設定");
        config.AppendLine("vault {");
        config.AppendLine($"  address = \"{_vaultAddress}\"");
        config.AppendLine("  retry {");
        config.AppendLine("    num_retries = 5");
        config.AppendLine("  }");
        config.AppendLine("}");
        config.AppendLine();

        config.AppendLine("# 自動認證設定");
        config.AppendLine("auto_auth {");
        config.AppendLine("  method \"approle\" {");
        config.AppendLine("    mount_path = \"auth/approle\"");
        config.AppendLine("    config = {");
        config.AppendLine($"      role_id_file_path   = \"{_roleIdPath.Replace("\\", "/")}\"");
        config.AppendLine($"      secret_id_file_path = \"{_secretIdPath.Replace("\\", "/")}\"");
        config.AppendLine("    }");
        config.AppendLine("  }");
        config.AppendLine();
        
        config.AppendLine("  sink \"file\" {");
        config.AppendLine("    config = {");
        config.AppendLine("      path = \"./vault-token\"");
        config.AppendLine("      mode = 0644");
        config.AppendLine("    }");
        config.AppendLine("  }");
        config.AppendLine("}");
        config.AppendLine();

        config.AppendLine("# API 代理設定");
        config.AppendLine("api_proxy {");
        config.AppendLine("  use_auto_auth_token = true");
        config.AppendLine("}");
        config.AppendLine();

        config.AppendLine("# 監聽設定");
        config.AppendLine("listener \"tcp\" {");
        config.AppendLine("  address = \"127.0.0.1:8100\"");
        config.AppendLine("  tls_disable = true");
        config.AppendLine("}");
        config.AppendLine();

        config.AppendLine("# 快取設定");
        config.AppendLine("cache {");
        config.AppendLine("  use_auto_auth_token = true");
        config.AppendLine("}");
        config.AppendLine();

        config.AppendLine("# 模板設定 - ASP.NET Core appsettings.json");
        config.AppendLine("template {");
        config.AppendLine("  source      = \"./templates/appsettings.json.tpl\"");
        config.AppendLine("  destination = \"./config/appsettings.json\"");
        config.AppendLine("  perms       = 0644");
        config.AppendLine("  command     = \"echo Configuration updated at $(Get-Date)\"");
        config.AppendLine("}");
        config.AppendLine();

        config.AppendLine("# 模板設定 - 連接字串配置");
        config.AppendLine("template {");
        config.AppendLine("  source      = \"./templates/connectionstrings.json.tpl\"");
        config.AppendLine("  destination = \"./config/connectionstrings.json\"");
        config.AppendLine("  perms       = 0644");
        config.AppendLine("}");

        await File.WriteAllTextAsync("vault-agent.hcl", config.ToString());
        Console.WriteLine("生成 vault-agent.hcl");
    }

    private async Task GenerateTemplatesAsync()
    {
        // ASP.NET Core appsettings.json 模板
        var appSettingsTemplate = """
            {{- with secret "secret/data/app-dev/database" -}}
            {
              "Logging": {
                "LogLevel": {
                  "Default": "Information",
                  "Microsoft.AspNetCore": "Warning"
                }
              },
              "ConnectionStrings": {
                "DefaultConnection": "Server=localhost;Database=MyApp;User Id={{ .Data.data.account }};Password={{ .Data.data.password }};TrustServerCertificate=true;"
              },
              "DatabaseCredentials": {
                "Username": "{{ .Data.data.account }}",
                "Password": "{{ .Data.data.password }}"
              },
              "AllowedHosts": "*",
              "VaultAgent": {
                "LastUpdated": "{{ now.Format "2006-01-02 15:04:05" }}",
                "Environment": "Development"
              }
            }
            {{- end }}
            """;

        await File.WriteAllTextAsync("templates/appsettings.json.tpl", appSettingsTemplate);
        Console.WriteLine("生成 templates/appsettings.json.tpl");

        // 連接字串專用模板
        var connectionStringsTemplate = """
            {{- with secret "secret/data/app-dev/database" -}}
            {
              "ConnectionStrings": {
                "DefaultConnection": "Server=localhost;Database=MyApp;User Id={{ .Data.data.account }};Password={{ .Data.data.password }};TrustServerCertificate=true;",
                "ReadOnlyConnection": "Server=localhost;Database=MyApp;User Id={{ .Data.data.account }};Password={{ .Data.data.password }};TrustServerCertificate=true;ApplicationIntent=ReadOnly;"
              },
              "Credentials": {
                "Account": "{{ .Data.data.account }}",
                "Password": "{{ .Data.data.password }}"
              },
              "Metadata": {
                "GeneratedAt": "{{ now.Format "2006-01-02 15:04:05" }}",
                "Source": "Vault Agent"
              }
            }
            {{- end }}
            """;

        await File.WriteAllTextAsync("templates/connectionstrings.json.tpl", connectionStringsTemplate);
        Console.WriteLine("生成 templates/connectionstrings.json.tpl");
    }

    public string GetVaultAgentStartCommand()
    {
        return "vault agent -config=vault-agent.hcl";
    }

    public void PrintUsageInstructions()
    {
        Console.WriteLine();
        Console.WriteLine("=== Vault Agent 使用說明 ===");
        Console.WriteLine("1. 啟動 Vault Agent:");
        Console.WriteLine("   vault agent -config=vault-agent.hcl");
        Console.WriteLine();
        Console.WriteLine("2. Vault Agent 將會:");
        Console.WriteLine("   - 自動使用 AppRole 認證");
        Console.WriteLine("   - 生成並維護 vault-token");
        Console.WriteLine("   - 監聽在 127.0.0.1:8100");
        Console.WriteLine("   - 動態更新配置文件");
        Console.WriteLine();
        Console.WriteLine("3. 生成的文件:");
        Console.WriteLine("   - config/appsettings.json (ASP.NET Core 配置)");
        Console.WriteLine("   - config/connectionstrings.json (連接字串配置)");
        Console.WriteLine("   - vault-token (自動獲取的認證 token)");
        Console.WriteLine();
        Console.WriteLine("4. 在 C# 應用程式中使用:");
        Console.WriteLine("   - 直接讀取 config/appsettings.json");
        Console.WriteLine("   - 或透過 HTTP API 存取 http://127.0.0.1:8100");
    }
}