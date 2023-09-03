using Lab.ErrorHandler.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Lab.ErrorHandler.API.Controllers;

[ApiController]
[Route("[controller]")]
public class MembersController : GenericController
{
    private readonly ILogger<MembersController> _logger;
    private readonly MemberService3 _memberService;

    public MembersController(ILogger<MembersController> logger,
        MemberService3 memberService)
    {
        this._logger = logger;
        this._memberService = memberService;
    }

    [Produces("application/json")]
    [HttpPost("{memberId}/bind-cellphone", Name = "BindCellphone")]
    [ProducesResponseType(typeof(Failure), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Post(int memberId,
        BindCellphoneRequest request,
        CancellationToken cancel = default)
    {
        request.MemberId = memberId;
        var createMemberResult =
            await this._memberService.BindCellphoneAsync(request, cancel);
        if (createMemberResult.Failure != null)
        {
            this._logger.LogInformation(500, "Bind cellphone failure:{@Failure}", createMemberResult.Failure);
            return this.FailureContent(createMemberResult.Failure);
        }

        return this.NoContent();
    }
}