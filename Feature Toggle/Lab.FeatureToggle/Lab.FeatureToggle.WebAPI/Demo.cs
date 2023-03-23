using Microsoft.FeatureManagement;

namespace Lab.FeatureToggle.WebAPI;

public class Demo
{
    private readonly IFeatureManager _featureManager;

    public Demo(IFeatureManager featureManager)
    {
        this._featureManager = featureManager;
    }

    public async Task<string> CreateFeatureA()
    {
        if (await this._featureManager.IsEnabledAsync(FeatureFlags.FeatureA))
        {
            //do something
            return "OK";
        }

        return null;
    }
    public async Task<string> CreateFeatureB()
    {
        if (await this._featureManager.IsEnabledAsync(FeatureFlags.FeatureB))
        {
            //do something
            return "OK";
        }

        return null;
    }

}