using Lab.Sharding.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Lab.Sharding.WebAPI.Member;

[ApiController]
public class MemberController(
	MemberHandler memberHandler) : ControllerBase
{
	[HttpGet]
	[Route("api/v1/members:offset", Name = "GetMemberOffset")]
	public async Task<ActionResult<PaginatedList<GetMemberResponse>>> GetMemberOffset(
		CancellationToken cancel = default)
	{
		var pageSize = 10;
		var pageIndex = 0;
		var noCache = true;
		if (this.Request.Headers.TryGetValue("x-page-index", out var pageIndexText))
		{
			int.TryParse(pageIndexText, out pageIndex);
		}

		if (this.Request.Headers.TryGetValue("x-page-size", out var pageSizeText))
		{
			int.TryParse(pageSizeText, out pageSize);
		}

		if (this.Request.Headers.TryGetValue("cache-control", out var noCacheText))
		{
			noCache = string.Compare(noCacheText, "no-cache", StringComparison.InvariantCultureIgnoreCase) == 0;
		}

		var result = await memberHandler.GetMembersAsync(pageIndex, pageSize, noCache, cancel);
		return this.Ok(result);
	}
}