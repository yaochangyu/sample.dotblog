using IdempotencyKey.WebApi.IdempotencyKeys;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;

namespace IdempotencyKey.WebApi.Members;

[ApiController]
[Route("api/members")]
public class MemberController(MemberHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await handler.GetAllAsync(ct);
        return ToActionResult(result, Ok);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await handler.GetByIdAsync(id, ct);
        return ToActionResult(result, Ok);
    }

    [HttpPost]
    [IdempotencyKey]
    public async Task<IActionResult> Create(CreateMemberRequest request, CancellationToken ct)
    {
        var result = await handler.CreateAsync(request, ct);
        return ToActionResult(result, m => CreatedAtAction(nameof(GetById), new { id = m.Id }, m));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMemberRequest request, CancellationToken ct)
    {
        var result = await handler.UpdateAsync(id, request, ct);
        return ToActionResult(result, Ok);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await handler.DeleteAsync(id, ct);
        return ToActionResult(result, _ => NoContent());
    }

    private IActionResult ToActionResult<T>(Result<T, Failure> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsFailure)
        {
            if (result.Error.IsRetryable)
                HttpContext.Items["Idempotency:ShouldDeleteKey"] = true;

            return StatusCode((int)FailureCodeMapper.GetHttpStatusCode(result.Error), result.Error);
        }
        return onSuccess(result.Value);
    }
}

