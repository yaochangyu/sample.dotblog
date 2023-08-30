using FluentValidation;
using Lab.ModelValidation.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lab.ModelValidation.API.Controllers;

[ApiController]
[Route("[controller]")]
public class MembersController : GenericController
{
    private readonly ILogger<MembersController> _logger;
    private readonly IValidator<CreateMemberRequest> _validator;
    private readonly MemberService _memberService;

    public MembersController(ILogger<MembersController> logger,
        IValidator<CreateMemberRequest> validator,
        MemberService memberService)
    {
        this._logger = logger;
        this._validator = validator;
        this._memberService = memberService;
    }

    // [ModelValidation()]
    [HttpPost(Name = "CreateData")]
    public async Task<ActionResult> Post(CreateMemberRequest request,
        CancellationToken cancel)
    {
        var createMemberResult = await this._memberService.CreateMemberAsync(request, cancel);
        if (createMemberResult.Failure != null)
        {
            return this.GenericFailure(createMemberResult.Failure);
        }

        return this.NoContent();
    }

    [HttpGet("{id}", Name = "GetData")]
    public ActionResult Get(int id)
    {
        var service = new MemberServiceTemp();
        var (failure, _) = service.GetMember(id);
        if (failure != null)
        {
            return this.GenericFailure(failure);
        }

        return this.NoContent();
    }
}