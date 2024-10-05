using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines;

namespace Lab.HashiCorpVault.Test;

[TestClass]
public class UnitTest1
{
    string VaultToken = "hvs.X75F3khrl1h6hqCHB8BZbqkf";

    [TestMethod]
    public async Task 讀寫KV_V1()
    {
        // 設定 Vault Server 和 Token
        var vaultServer = "http://127.0.0.1:8200";

        // var vaultToken = "hvs.VEQZGVYiD6cO1CeSGFtbSpI4";

        // 初始化 Vault Client
        var authMethod = new TokenAuthMethodInfo(this.VaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        // 寫入 Secrets (等同於 vault kv put)
        var secretData = new Dictionary<string, object>
        {
            { "username", "admin" },
            { "password", "123456" }
        };

        var secretPath = "my-secret";
        var mount = "chechia-net/sre/workshop";

        await vaultClient.V1.Secrets.KeyValue.V1.WriteSecretAsync(secretPath, secretData, mountPoint: mount);
        var secret = await vaultClient.V1.Secrets.KeyValue.V1.ReadSecretAsync(secretPath, mountPoint: mount);
        Console.WriteLine("Secrets 已成功寫入！");
        Console.WriteLine($"username={secret.Data["username"]}");
        Console.WriteLine($"password={secret.Data["password"]}");
    }

    [TestMethod]
    public async Task 讀寫KV_V2()
    {
        // 設定 Vault Server 和 Token
        var vaultServer = "http://127.0.0.1:8200";

        // 初始化 Vault Client
        var authMethod = new TokenAuthMethodInfo(this.VaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        // 寫入 Secrets (等同於 vault kv put)
        var secretData = new Dictionary<string, object>
        {
            { "username", "admin" },
            { "password", "123456" }
        };

        var secretPath = "my-secret";
        var mount = "chechia-net/sre/workshop";

        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(secretPath, secretData, mountPoint: mount);
        Console.WriteLine("Secrets 已成功寫入！");
        var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath, mountPoint: mount);
        Console.WriteLine($"username={secret.Data.Data["username"]}");
        Console.WriteLine($"password={secret.Data.Data["password"]}");
    }

    [TestMethod]
    public async Task 建立KV_V2()
    {
        // 設定 Vault Server 和 Token
        var vaultServer = "http://127.0.0.1:8200";

        // 初始化 Vault Client
        var authMethod = new TokenAuthMethodInfo(VaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        // 啟用 KV V2 存儲引擎
        var path = "chechia-net/sre/workshop"; // 要啟用的路徑
        await CreateKvV2Path(vaultClient, path);
        Console.WriteLine($"KV V2 secrets engine enabled at path: {path}");
    }

    static async Task CreateKvV2Path(IVaultClient vaultClient, string path)
    {
        try
        {
            await vaultClient.V1.System.MountSecretBackendAsync(new SecretsEngine
            {
                Path = path,
                Type = new SecretsEngineType("kv-v2")
            });

            Console.WriteLine($"Successfully created kv-v2 path: {path}");
        }
        catch (VaultApiException ex)
        {
            if (ex.Message.Contains("path is already in use"))
            {
                Console.WriteLine($"The path '{path}' is already in use. It may already be mounted.");
            }
            else
            {
                Console.WriteLine($"Error creating kv-v2 path: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}