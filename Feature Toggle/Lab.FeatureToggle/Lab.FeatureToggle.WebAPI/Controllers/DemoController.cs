using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

namespace Lab.FeatureToggle.WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{

    private readonly ILogger<DemoController> _logger;
    private readonly IFeatureManager _featureManager;
    public DemoController(ILogger<DemoController> logger,
        IFeatureManager featureManager)
    {
        _logger = logger;
        this._featureManager = featureManager;
    }

    [HttpGet]
    [FeatureGate(FeatureFlags.FeatureB)]
    public async Task<ActionResult> Get()
    {
        if (await _featureManager.IsEnabledAsync(FeatureFlags.FeatureA))
        {
            // Run the following code
        }

        return this.Ok();
    }
}

