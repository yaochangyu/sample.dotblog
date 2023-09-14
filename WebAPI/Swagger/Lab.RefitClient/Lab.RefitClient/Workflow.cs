using Lab.RefitClient.GeneratedCode.PetStore;

namespace Lab.RefitClient;

public class Workflow
{
    readonly ISwaggerPetstoreOpenAPI30 _petStore;

    public Workflow(ISwaggerPetstoreOpenAPI30 petStore)
    {
        this._petStore = petStore;
    }
    
    public async Task<string> GetUser(string name)
    {
        var getUserByNameResult = await this._petStore.GetUserByName(name);
        return getUserByNameResult.Content.Username;
    }
}