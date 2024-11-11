using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lab.CursorPaging.WebApi.Member.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lab.CursorPaging.WebApi.Controllers;

[ApiController]
public partial class MembersController2 : ControllerBase
{
    private readonly IDbContextFactory<MemberDbContext> _memberDbContextFactory;

    public MembersController2(IDbContextFactory<MemberDbContext> memberDbContextFactory)
    {
        this._memberDbContextFactory = memberDbContextFactory;
    }

    [HttpGet]
    [Route("/api/v2/members:cursor")]
    public async Task<ActionResult<CursorPagination<MemberDataEntity>>> GetCursor()
    {
        var pageSize = this.TryGetPageSize();

        // var isPreviousPage = this.TryGetPrevious();
        var nextCursorToken = this.TryGetNextCursorToken();
        var previousCursorToken = this.TryGetPreviousCursorToken();
        var result = new CursorPagination<MemberDataEntity>()
        {
            NextCursorToken = nextCursorToken,
            PreviousCursorToken = previousCursorToken
        };
        await using var dbContext = await this._memberDbContextFactory.CreateDbContextAsync();
        var query = dbContext.Members.Select(p => p);

        if (string.IsNullOrWhiteSpace(nextCursorToken) == false)
        {
            var decodePageToken = DecodePageToken(nextCursorToken);
            var sequenceId = decodePageToken.sequenceId;

            query = query.Where(x => x.SequenceId > sequenceId)
                ;
        }
        else if (string.IsNullOrWhiteSpace(previousCursorToken) == false)
        {
            var decodePageToken = DecodePageToken(previousCursorToken);
            var sequenceId = decodePageToken.sequenceId;
            var start = sequenceId - pageSize;
            query = query.Where(x => x.SequenceId > start);
            query = query.Where(x => x.SequenceId <= sequenceId);
        }
        
        query = query.Take(pageSize + 1);
        var items = await query.AsNoTracking().ToListAsync();

        if (nextCursorToken != null)
        {
            var hasNextPage = items.Count > pageSize;
            if (hasNextPage)
            {
                items.RemoveAt(items.Count - 1);
                var next = items.Last();
                var encodedToken = EncodePageToken(next.Id, next.SequenceId);
                this.Response.Headers.Add("X-Next-Cursor-Token", encodedToken);
                result.NextCursorToken = encodedToken;
            }
            else
            {
                result.NextCursorToken = null;
            }

            result.PreviousCursorToken = nextCursorToken;
        }
        else if (previousCursorToken != null)
        {
            var prev = items.First();
            var prevToken = EncodePageToken(prev.Id, prev.SequenceId);
            this.Response.Headers.Add("X-Previous-Cursor-Token", prevToken);
            result.PreviousCursorToken = prevToken;
            result.NextCursorToken = previousCursorToken;
        }
        else
        {
            // 第一頁
            var hasNextPage = items.Count > pageSize;
            if (hasNextPage)
            {
                items.RemoveAt(items.Count - 1);
                var next = items.Last();
                var encodedToken = EncodePageToken(next.Id, next.SequenceId);
                this.Response.Headers.Add("X-Next-Cursor-Token", encodedToken);
                result.NextCursorToken = encodedToken;
                result.PreviousCursorToken = null;
            }
            else
            {
                result.NextCursorToken = null;
            }
        }

        result.Items = items;
        return this.Ok(result);
    }

    private string TryGetNextCursorToken()
    {
        string result = null;
        if (this.Request.Headers.TryGetValue("X-Next-Cursor-Token", out var data))
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

    public static (string id, long sequenceId) DecodePageToken(string cursorToken)
    {
        if (string.IsNullOrEmpty(cursorToken))
        {
            return (null, 0);
        }

        string id = null;
        long sequenceId = 0;
        var base64Bytes = Convert.FromBase64String(cursorToken);
        var json = Encoding.UTF8.GetString(base64Bytes);
        var jsonNode = JsonNode.Parse(json);
        JsonObject jsonObject = jsonNode.AsObject();

        if (jsonObject.TryGetPropertyValue("sequenceId", out var sequenceIdNode))
        {
            sequenceId = sequenceIdNode.GetValue<long>();
        }

        if (jsonObject.TryGetPropertyValue("id", out var idNode))
        {
            id = idNode.GetValue<string>();
        }

        return (id, sequenceId);
    }

    public static string EncodePageToken(string? id,
                                         long? sequenceId)
    {
        if (id == null || sequenceId == null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(new { id, sequenceId });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}