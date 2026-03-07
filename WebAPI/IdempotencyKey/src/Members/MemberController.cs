using Microsoft.AspNetCore.Mvc;

namespace IdempotencyKey.WebApi.Members;

[ApiController]
[Route("api/members")]
public class MemberController(IMemberRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Member>>> GetAll() =>
        Ok(await repository.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Member>> GetById(Guid id)
    {
        var member = await repository.GetByIdAsync(id);
        return member is null ? NotFound() : Ok(member);
    }

    [HttpPost]
    public async Task<ActionResult<Member>> Create(CreateMemberRequest request)
    {
        var member = new Member
        {
            Name = request.Name,
            Email = request.Email
        };

        await repository.AddAsync(member);

        return CreatedAtAction(nameof(GetById), new { id = member.Id }, member);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Member>> Update(Guid id, UpdateMemberRequest request)
    {
        var member = await repository.UpdateAsync(id, request);
        return member is null ? NotFound() : Ok(member);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
