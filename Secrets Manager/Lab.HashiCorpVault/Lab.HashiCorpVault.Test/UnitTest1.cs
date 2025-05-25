using System.Runtime.InteropServices;
using System.Security;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines;
using VaultSharp.V1.SystemBackend;

namespace Lab.HashiCorpVault.Test;

[TestClass]
public class UnitTest1
{
    private readonly string VaultToken = "你的 token";
    private readonly string VaultServer = "http://127.0.0.1:8200";

    [TestMethod]
    public async Task 設定多個原則()
    {
        var setup = new VaultSetup(VaultServer, VaultToken);
        await setup.SetupVaultAsync();
    }

    [TestMethod]
    public async Task 讀寫KV_V1()
    {
        // 初始化 Vault Client
        var vaultClient = this.CreateVaultClient();

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
    public async Task 建立KV_V2_Engine()
    {
        // 初始化 Vault Client
        var vaultClient = this.CreateVaultClient();

        // 啟用 KV V2 存儲引擎
        var mountPath = "job/dream-team"; // 要啟用的路徑
        await CreateKvV2Path(vaultClient, mountPath);
        Console.WriteLine($"KV V2 secrets engine enabled at path: {mountPath}");
    }

    [TestMethod]
    public async Task 讀寫KV_V2_Secret()
    {
        // 初始化 Vault Client
        var vaultClient = this.CreateVaultClient();

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
    public async Task 停用KV_V2_Secret()
    {
        // 初始化 Vault Client
        var vaultClient = this.CreateVaultClient();

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
    public async Task 建立Policy()
    {
        // 初始化 Vault Client
        var vaultClient = this.CreateVaultClient();
        var path = "job/dream-team/my-secret";

        // 定義策略
        var policyName = "my-policy1";
        var policyRules = """
                          path "job/dream-team/*" {
                              capabilities = ["create", "read", "update", "delete", "list"]
                          }
                          """;
        // 創建或更新策略
        await vaultClient.V1.System.WritePolicyAsync(new Policy
        {
            Name = policyName,
            Rules = policyRules
        });
    }

    [TestMethod]
    public async Task 建立TokenAuth()
    {
        // 初始化 Vault Client
        var vaultClient = this.CreateVaultClient();

        // 建立 Token 認證方法
        var tokenResponse = await vaultClient.V1.Auth.Token.CreateTokenAsync(new CreateTokenRequest()
        {
            Policies = new List<string> { "my-policy1" },
        });
        string clientToken = tokenResponse.AuthInfo.ClientToken;
        Console.WriteLine($"token created: {clientToken}");

        // 使用新建立的 Token 來創建新的 Vault Client
        var newVaultClient = new VaultClient(new VaultClientSettings(this.VaultServer, new TokenAuthMethodInfo(clientToken)));

        // 驗證新 Vault Client 是否能夠讀取 Secret
        var secretPath = "my-secret";
        var mountPath = "job/dream-team";

        var secret = await newVaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(secretPath, mountPoint: mountPath);
        Console.WriteLine($"username={secret.Data.Data["username"]}");
        Console.WriteLine($"password={secret.Data.Data["password"]}");
    }

    [TestMethod]
    public async Task 讀寫KV_SecureString()
    {
        // 初始化 Vault Client
        var vaultClient = this.CreateVaultClient();

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

    private VaultClient CreateVaultClient()
    {
        var authMethod = new TokenAuthMethodInfo(this.VaultToken);
        var vaultClientSettings = new VaultClientSettings(this.VaultServer, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);
        return vaultClient;
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
