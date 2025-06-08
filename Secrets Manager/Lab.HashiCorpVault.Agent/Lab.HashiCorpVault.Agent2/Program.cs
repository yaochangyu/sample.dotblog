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
        vaultApiClient.UpdateToken(agentToken);
        var secretResult = await vaultApiClient.GetSecretAsync("dev/data/db/connection/identity");
        var secretData = secretResult["data"]?["data"] as JsonObject;
        if (secretData == null)
        {
            Console.WriteLine("Error: Unable to read secret data.");
            return;
        }
        if (secretData.ContainsKey("Account") && secretData.ContainsKey("Password"))
        {
            Console.WriteLine($"Account: {secretData["Account"]}");
            Console.WriteLine($"Password: {secretData["Password"]}");
            Console.WriteLine("讀取秘密成功");
        }
        else
        {
            Console.WriteLine("Error: Account or Password not found in secret data.");
        }
    }
}
