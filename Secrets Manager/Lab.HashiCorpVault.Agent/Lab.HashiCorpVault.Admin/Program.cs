﻿using System.Text.Json.Nodes;
using Lab.HashiCorpVault.Client;

namespace Lab.HashiCorpVault.Admin;

class Program
{
    private static string VaultServer = "http://127.0.0.1:8200";
    private static string VaultRootToken = "你的 Vault Root Token";
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("Setup Vault Starting...");
        Console.Write("Please enter your Vault root token: ");
        var vaultRootToken = string.Empty;
        
        // 讀取 .vault-token 檔案，取得 root token
        var rootVaultFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".vault-token");

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
            Console.WriteLine("Error: Vault token cannot be empty.");
            return;
        }
        
        var vaultApiClient = new VaultApiClient(new HttpClient
        {
            BaseAddress = new Uri(VaultServer),
        }, vaultRootToken);
        var setup = new VaultAppRoleSetup2(vaultApiClient,vaultRootToken);

        // var setup = new VaultAppRoleSetup(VaultServer, vaultRootToken);
        
        await setup.SetupVaultAsync();
        // 建立管理者 Token
        var adminTokens = await setup.CreateAdminToken();
        
        // 測試使用管理者 Token 取得 app-dev 的 AppRole 認證資訊
        string adminToken = adminTokens["app-admin"];
        var id = await setup.GetAppRoleCredentialsAsync(adminToken, "app-dev");
        
        Console.WriteLine($"Role ID: {id.RoleId}, Secret ID: {id.SecretId}");
        
        // 印出管理者 Token
        string tokenString = string.Join(", ", adminTokens.Select(t => $"{t.Key} token: {t.Value}"));
        Console.WriteLine($"{tokenString}");
        var fileName = $"admin-token-{DateTime.UtcNow:yyyyMMddHHmmss}";
        await File.WriteAllTextAsync(fileName, adminToken);
        Console.WriteLine("Setup Vault Completed.");
    }
}
