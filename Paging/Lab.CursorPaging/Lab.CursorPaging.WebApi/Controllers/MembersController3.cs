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
    public async Task<ActionResult<CursorPagination<MemberDataEntity>>> GetCursor3()
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

    private async Task<CursorPagination<MemberDataEntity>> GetPaginatedResultsAsync(IQueryable<MemberDataEntity> query,
        int pageSize,
        string cursor = null,
        bool isPreviousPage = false)
    {
        if (!string.IsNullOrEmpty(cursor))
        {
            var cursorValue = Convert.FromBase64String(cursor);
            var cursorId = BitConverter.ToInt32(cursorValue, 0);

            if (isPreviousPage)
            {
                query = query.Where(x => x.SequenceId < cursorId).OrderByDescending(x => x.SequenceId);
            }
            else
            {
                query = query.Where(x => x.SequenceId > cursorId).OrderBy(x => x.SequenceId);
            }
        }
        else
        {
            query = query.OrderBy(x => x.Id);
        }

        // 取得查詢結果並確保順序
        var items = await query.Take(pageSize).ToListAsync();
        if (isPreviousPage)
        {
            items.Reverse(); // 反轉以符合遞增順序
        }

        // 計算下一頁和上一頁的游標
        var nextCursor = items.Any()
            ? Convert.ToBase64String(BitConverter.GetBytes(items.Last().SequenceId))
            : null;
        var prevCursor = items.Any()
            ? Convert.ToBase64String(BitConverter.GetBytes(items.First().SequenceId))
            : null;

        return new CursorPagination<MemberDataEntity>
        {
            Items = items,
            NextCursorToken = nextCursor,
            PreviousCursorToken = prevCursor,
            PageSize = pageSize
        };
    }
}