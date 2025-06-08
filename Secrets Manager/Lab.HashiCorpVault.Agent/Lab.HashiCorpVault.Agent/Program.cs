namespace Lab.HashiCorpVault.Agent;

class Program
{
    private static string VaultServer = "http://127.0.0.1:8200";
    private static string VaultToken = "你的 token";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Vault Agent Starting...");
        //觀察 Vault 的進程
        await VaultAgentSetup.VerifyVaultProcessInfoAsync();
        
        Console.Write("Please enter your Vault token: ");
        string vaultToken = Console.ReadLine() ?? VaultToken;
        
        if (string.IsNullOrWhiteSpace(vaultToken))
        {
            Console.WriteLine("Error: Vault token cannot be empty.");
            return;
        }

        var setup = new VaultAgentSetup(VaultServer, vaultToken);
        await setup.SetupVaultAgentAsync();
        
        //觀察 Vault 的進程
        await VaultAgentSetup.VerifyVaultProcessInfoAsync();
    }
}
