using Lab.RefitClient.GeneratedCode.PetStore;
using Microsoft.AspNetCore.Mvc;

namespace Lab.RefitClient.WebAPI2.Controllers;

[ApiController]
[Route("[controller]")]
public class DemoController : ControllerBase
{
    private readonly ISwaggerPetstoreOpenAPI30 _petStoreService;
    private readonly ILogger<DemoController> _logger;

    public DemoController(ILogger<DemoController> logger,
        ISwaggerPetstoreOpenAPI30 petStoreService)
    {
        this._logger = logger;
        this._petStoreService = petStoreService;
    }

    [HttpGet("{name}", Name = "GetUserName")]
    public async Task<ActionResult> Get(string name)
    {
        var response = await this._petStoreService.GetUserByName(name);
        var user = response.Content;
        var idempotencyKey = response.Headers.GetValues(PetStoreHeaderNames.IdempotencyKey).FirstOrDefault();
        var apiKey = response.Headers.GetValues(PetStoreHeaderNames.ApiKey).FirstOrDefault();
        this.Response.Headers[PetStoreHeaderNames.IdempotencyKey]= idempotencyKey;
        this.Response.Headers[PetStoreHeaderNames.ApiKey]= apiKey;
        return this.Ok(user);
    }
}