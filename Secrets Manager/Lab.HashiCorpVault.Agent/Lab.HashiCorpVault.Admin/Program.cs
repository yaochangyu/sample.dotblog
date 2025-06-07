namespace Lab.HashiCorpVault.Admin;

class Program
{
    private static string VaultServer = "http://127.0.0.1:8200";
    private static string VaultRootToken = "你的 Vault Root Token，這裡替換掉";

    static async Task Main(string[] args)
    {
        Console.WriteLine("Setup Vault Starting...");
        
        var setup = new VaultAgentSetup(VaultServer, VaultRootToken);
        await setup.SetupVaultAgentAsync();
    }
}
