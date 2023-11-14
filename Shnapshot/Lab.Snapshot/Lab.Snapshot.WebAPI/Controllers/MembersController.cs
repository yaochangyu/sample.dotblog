using System.Net;
using Lab.Snapshot.WebAPI.ServiceModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Lab.Snapshot.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly ILogger<MembersController> _logger;
    readonly MemberRepository _memberRepository;

    public MembersController(ILogger<MembersController> logger, MemberRepository memberRepository)
    {
        this._logger = logger;
        this._memberRepository = memberRepository;
    }

    [HttpPost(Name = "InsertMember")]
    public async Task<ActionResult<MemberResponse>> Post(InsertMemberRequest request)
    {
        var insertMemberResult = await this._memberRepository.InsertMemberAsync(request);
        if (insertMemberResult.Failure != null)
        {
            return new ObjectResult(insertMemberResult.Failure)
            {
                ContentTypes = new MediaTypeCollection()
                {
                    "application/json"
                },
                StatusCode = (int)HttpStatusCode.BadRequest
            };
        }

        return this.NoContent();
    }

    [HttpPut("{currentAccount}/bind", Name = "BindMember")]
    public async Task<ActionResult<MemberResponse>> Bind(string currentAccount, UpdateMemberRequest request)
    {
        var bindMemberResult = await this._memberRepository.BindMemberAsync(currentAccount, request);
        if (bindMemberResult.Failure != null)
        {
            return new ObjectResult(bindMemberResult.Failure)
            {
                ContentTypes = new MediaTypeCollection()
                {
                    "application/json"
                },
                StatusCode = (int)HttpStatusCode.BadRequest
            };
        }

        return this.NoContent();
    }

    [HttpGet("{account}", Name = "GetMemberByAccount")]
    public async Task<ActionResult<MemberResponse>> Get(string account, int? version)
    {
        var getMemberResult = await this._memberRepository.GetMemberAsync(account, version);
        if (getMemberResult == null)
        {
            return this.NotFound();
        }

        return this.Ok(getMemberResult);
    }
}