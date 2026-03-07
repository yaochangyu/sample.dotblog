using IdempotencyKey.WebApi.IdempotencyKeys;
using Microsoft.AspNetCore.Mvc;

namespace IdempotencyKey.WebApi.Members;

[ApiController]
[Route("api/members")]
public class MemberController(MemberHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await handler.GetAllAsync(ct);
        if (result.IsFailure)
            return StatusCode((int)FailureCodeMapper.GetHttpStatusCode(result.Error), result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await handler.GetByIdAsync(id, ct);
        if (result.IsFailure)
            return StatusCode((int)FailureCodeMapper.GetHttpStatusCode(result.Error), result.Error);
        return Ok(result.Value);
    }

    [HttpPost]
    [IdempotencyKey]
    public async Task<IActionResult> Create(CreateMemberRequest request, CancellationToken ct)
    {
        var result = await handler.CreateAsync(request, ct);
        if (result.IsFailure)
            return StatusCode((int)FailureCodeMapper.GetHttpStatusCode(result.Error), result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMemberRequest request, CancellationToken ct)
    {
        var result = await handler.UpdateAsync(id, request, ct);
        if (result.IsFailure)
            return StatusCode((int)FailureCodeMapper.GetHttpStatusCode(result.Error), result.Error);
        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await handler.DeleteAsync(id, ct);
        if (result.IsFailure)
            return StatusCode((int)FailureCodeMapper.GetHttpStatusCode(result.Error), result.Error);
        return NoContent();
    }
}

