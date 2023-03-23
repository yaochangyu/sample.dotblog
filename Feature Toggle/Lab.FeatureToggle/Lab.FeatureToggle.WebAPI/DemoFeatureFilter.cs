using Microsoft.FeatureManagement;

namespace Lab.FeatureToggle.WebAPI;

public class DemoFeatureFilter : IFeatureFilter
{
    public async Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context)
    {
        // Your implementation here
        return true;
    }
}