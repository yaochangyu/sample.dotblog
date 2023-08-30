using Lab.ModelValidation.API.Filters;
using Lab.ModelValidation.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lab.ModelValidation.API.Controllers;

[ApiController]
[Route("[controller]")]
public class MembersController : GenericController
{
    private readonly ILogger<MembersController> _logger;

    public MembersController(ILogger<MembersController> logger)
    {
        this._logger = logger;
    }

    // [ModelValidation()]
    [HttpPost(Name = "CreateData")]
    public ActionResult Post(CreateMemberRequest request)
    {
        return this.NoContent();
    }

    [HttpGet("{id}", Name = "GetData")]
    public ActionResult Get(int id)
    {
        var service = new MemberService();
        var (failure, _) = service.GetMember(id);
        if (failure != null)
        {
            return this.GenericFailure(failure);
        }

        return this.NoContent();
    }
}