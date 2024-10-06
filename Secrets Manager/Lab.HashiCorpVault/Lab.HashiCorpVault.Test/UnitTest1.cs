using System.Runtime.InteropServices;
using System.Security;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines;

namespace Lab.HashiCorpVault.Test;

[TestClass]
public class UnitTest1
{
    private readonly string VaultToken = "你的 token";

    [TestMethod]
    public async Task 讀寫KV_V1()
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
        var mountPath = "chechia-net/sre/workshop";

        await vaultClient.V1.Secrets.KeyValue.V1.WriteSecretAsync(secretPath, secretData, mountPath);
        Console.WriteLine("Secrets 已成功寫入！");

        Console.WriteLine("讀取KV-V1：");
        var secret = await vaultClient.V1.Secrets.KeyValue.V1.ReadSecretAsync(secretPath, mountPath);
        Console.WriteLine($"username={secret.Data["username"]}");
        Console.WriteLine($"password={secret.Data["password"]}");
    }

    [TestMethod]
    public async Task 建立KV_V2()
    {
        // 設定 Vault Server 和 Token
        var vaultServer = "http://127.0.0.1:8200";

        // 初始化 Vault Client
        var authMethod = new TokenAuthMethodInfo(this.VaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        // 啟用 KV V2 存儲引擎
        var mountPath = "job/dream-team"; // 要啟用的路徑
        await CreateKvV2Path(vaultClient, mountPath);
        Console.WriteLine($"KV V2 secrets engine enabled at path: {mountPath}");
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
        var mountPath = "job/dream-team";

        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(secretPath, secretData, mountPoint: mountPath);
        Console.WriteLine("Secrets 已成功寫入！");

        Console.WriteLine("讀取KV-V2：");
        var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath, mountPoint: mountPath);
        Console.WriteLine($"username={secret.Data.Data["username"]}");
        Console.WriteLine($"password={secret.Data.Data["password"]}");
    }

    [TestMethod]
    public async Task 停用KV_V2()
    {
        // 設定 Vault Server 和 Token
        var vaultServer = "http://127.0.0.1:8200";

        // 初始化 Vault Client
        var authMethod = new TokenAuthMethodInfo(this.VaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        var secretPath = "my-secret";
        var mountPath = "job/dream-team";
        await vaultClient.V1.System.UnmountSecretBackendAsync(mountPath);

        try
        {
            var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath, mountPoint: mountPath);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    [TestMethod]
    public async Task 讀寫KV_SecureString()
    {
        // 設定 Vault Server 和 Token
        var vaultServer = "http://127.0.0.1:8200";

        // 初始化 Vault Client
        var authMethod = new TokenAuthMethodInfo(this.VaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        // 寫入安全數據
        var secureData = new Dictionary<string, SecureString>
        {
            { "username", ConvertToSecureString("admin") },
            { "password", ConvertToSecureString("123456") }
        };
        var secretPath = "my-secret";
        var mount = "chechia-net/sre/workshop";
        await WriteSecureSecretAsync(vaultClient, secretPath, mount, secureData);
        Console.WriteLine("Secrets 已成功寫入！");

        // 讀取安全數據
        var retrievedData = await ReadSecureSecretAsync(vaultClient, secretPath, mount);

        // 安全地使用數據
        using (retrievedData["username"])
        using (retrievedData["password"])
        {
            // 在這裡使用安全字符串，避免將其轉換為普通字符串
            // 例如，可以將其傳遞給只接受 SecureString 的 API
            retrievedData.TryGetValue("username", out var username);
        }
    }

    private static async Task WriteSecureSecretAsync(IVaultClient vaultClient,
        string path,
        string mountPoint,
        Dictionary<string, SecureString> secureData)
    {
        var secretData = new Dictionary<string, object>();

        foreach (var kvp in secureData)
        {
            secretData[kvp.Key] = ConvertToUnsecureString(kvp.Value);
        }

        await vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(path, secretData, mountPoint: mountPoint);
    }

    private static async Task<Dictionary<string, SecureString>> ReadSecureSecretAsync(IVaultClient vaultClient,
        string path,
        string mountPoint)
    {
        var secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: mountPoint);
        var secureData = new Dictionary<string, SecureString>();

        foreach (var kvp in secret.Data.Data)
        {
            secureData[kvp.Key] = ConvertToSecureString(kvp.Value.ToString());
        }

        return secureData;
    }

    private static SecureString ConvertToSecureString(string plainText)
    {
        if (plainText == null)
            throw new ArgumentNullException(nameof(plainText));

        var secureString = new SecureString();
        foreach (char c in plainText)
        {
            secureString.AppendChar(c);
        }

        secureString.MakeReadOnly();
        return secureString;
    }

    private static string ConvertToUnsecureString(SecureString secureString)
    {
        if (secureString == null)
            throw new ArgumentNullException(nameof(secureString));

        IntPtr unmanagedString = IntPtr.Zero;
        try
        {
            unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            return Marshal.PtrToStringUni(unmanagedString);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
        }
    }

    private static async Task CreateKvV2Path(IVaultClient vaultClient, string path)
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