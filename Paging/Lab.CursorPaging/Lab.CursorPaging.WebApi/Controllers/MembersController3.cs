using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lab.CursorPaging.WebApi.Member.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab.CursorPaging.WebApi.Controllers;

[ApiController]
public partial class MembersController3 : ControllerBase
{
    private readonly IDbContextFactory<MemberDbContext> _memberDbContextFactory;

    public MembersController3(IDbContextFactory<MemberDbContext> memberDbContextFactory)
    {
        this._memberDbContextFactory = memberDbContextFactory;
    }

    [HttpGet]
    [Route("/api/v3/members:cursor")]
    public async Task<ActionResult<CursorPagination<MemberDataEntity>>> GetCursor()
    {
        var pageSize = this.TryGetPageSize();
        var cursorToken = this.TryGetCursorToken();
        var isPreviousCursor = this.TryGetPrevious();

        await using var dbContext = await this._memberDbContextFactory.CreateDbContextAsync();
        var query = dbContext.Members.Select(p => p);
        var paginatedResults =
            await this.GetPaginatedResultsAsync(query, pageSize, cursorToken, isPreviousCursor);

        return this.Ok(paginatedResults);
    }

    private string TryGetCursorToken()
    {
        string result = null;
        if (this.Request.Headers.TryGetValue("X-Cursor-Token", out var data))
        {
            result = data.FirstOrDefault();
        }

        return result;
    }

    private string TryGetPreviousCursorToken()
    {
        string result = null;
        if (this.Request.Headers.TryGetValue("X-Previous-Cursor-Token", out var data))
        {
            result = data.FirstOrDefault();
        }

        return result;
    }

    private bool TryGetPrevious()
    {
        var isPreviousPage = false;
        if (this.Request.Headers.TryGetValue("X-Previous", out var previousData))
        {
            bool.TryParse(previousData.FirstOrDefault(), out isPreviousPage);
        }

        return isPreviousPage;
    }

    private int TryGetPageSize() =>
        this.Request.Headers.TryGetValue("X-Page-Size", out var sizes)
            ? int.Parse(sizes.FirstOrDefault() ?? string.Empty)
            : 10;

    public async Task<CursorPagination<MemberDataEntity>> GetPaginatedResultsAsync(IQueryable<MemberDataEntity> query,
        int pageSize,
        string cursor = null,
        bool isPreviousPage = false)
    {
        if (!string.IsNullOrEmpty(cursor))
        {
            var cursorValue = Convert.FromBase64String(cursor);
            var cursorId = BitConverter.ToInt32(cursorValue, 0);

            query = isPreviousPage
                    ? query.Where(x => x.SequenceId < cursorId)
                    : query.Where(x => x.SequenceId > cursorId)
                ;
        }

        var items = await query.Take(pageSize).ToListAsync();

        var nextCursorToken = items.Any()
            ? Convert.ToBase64String(BitConverter.GetBytes(items.Last().SequenceId))
            : null;
        var prevCursorToken = items.Any()
            ? Convert.ToBase64String(BitConverter.GetBytes(items.First().SequenceId))
            : null;

        return new CursorPagination<MemberDataEntity>
        {
            Items = items,
            NextCursorToken = nextCursorToken,
            PreviousCursorToken = prevCursorToken,
            PageSize = pageSize
        };
    }
}