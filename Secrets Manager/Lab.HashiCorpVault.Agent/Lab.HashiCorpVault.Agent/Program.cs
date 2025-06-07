namespace Lab.HashiCorpVault.Agent;

class Program
{
    private static string VaultServer = "http://127.0.0.1:8200";
    private static string VaultRootToken = "你的 token";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Vault Agent Starting...");
        //觀察 Vault 的進程
        await VaultAgentSetup.VerifyVaultProcessInfoAsync();
        
        var setup = new VaultAgentSetup(VaultServer, VaultRootToken);
        await setup.SetupVaultAgentAsync();
        
        //觀察 Vault 的進程
        await VaultAgentSetup.VerifyVaultProcessInfoAsync();
    }
}
