using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lab.CursorPaging.WebApi.Member.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace Lab.CursorPaging.WebApi.Controllers;

[ApiController]
public partial class MembersController : ControllerBase
{
    private readonly IDbContextFactory<MemberDbContext> _memberDbContextFactory;

    public MembersController(IDbContextFactory<MemberDbContext> memberDbContextFactory)
    {
        this._memberDbContextFactory = memberDbContextFactory;
    }

    [HttpGet]
    [Route("/api/members")]
    public async Task<ActionResult<IEnumerable<MemberDataEntity>>> Get()
    {
        var pageSize = this.TryGetPageSize();
        var pageTokenResult = this.TryGetPageToken();
        var lastId = pageTokenResult.LastId;
        var lastSequenceId = pageTokenResult.LastSequenceId;

        await using var dbContext = await this._memberDbContextFactory.CreateDbContextAsync();
        var query = dbContext.Members.Select(p => p);
        if (string.IsNullOrWhiteSpace(lastId) == false)
        {
            query = query.Where(p => p.Id.CompareTo(lastId) > 0);
        }

        if (lastSequenceId.HasValue)
        {
            query = query.Where(p => p.SequenceId > lastSequenceId);
        }

        query = query.Take(pageSize + 1);
        var results = await query.AsNoTracking().ToListAsync();

        // 是否有下一頁
        bool hasNextPage = results.Count > pageSize;

        if (hasNextPage)
        {
            // 有下一頁，刪除最後一筆
            results.RemoveAt(results.Count - 1);

            // 產生下一頁的令牌
            var after = results.LastOrDefault();
            if (after != null)
            {
                var nextToken = EncodePageToken(after.Id, after.SequenceId);
                this.Response.Headers.Add("X-Next-Page-Token", nextToken);
            }
        }

        return this.Ok(results);
    }

    private int TryGetPageSize() =>
        this.Request.Headers.TryGetValue("X-Page-Size", out var sizes)
            ? int.Parse(sizes.FirstOrDefault() ?? string.Empty)
            : 10;

    private (string? LastId, long? LastSequenceId) TryGetPageToken()
    {
        if (this.Request.Headers.TryGetValue("X-Next-Page-Token", out var nextToken))
        {
            var decodeResult = DecodePageToken(nextToken);
            return (decodeResult.lastId, decodeResult.lastSequenceId);
        }

        return (null, null);
    }

    // 將 Id 和 Version 轉換為下一頁的令牌
    private static string EncodePageToken(string? lastId, long? lastSequenceId)
    {
        if (lastId == null || lastSequenceId == null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(new { lastId, lastSequenceId });
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    // 將下一頁的令牌解碼為 Id 和 Version
    private static (string lastId, long lastSequenceId) DecodePageToken(string nextToken)
    {
        if (string.IsNullOrEmpty(nextToken))
        {
            return (null, 0);
        }

        string lastId = null;
        long lastSequenceId = 0;
        var base64Bytes = Convert.FromBase64String(nextToken);
        var json = Encoding.UTF8.GetString(base64Bytes);
        var jsonNode = JsonNode.Parse(json);
        var jsonObject = jsonNode.AsObject();
        if (jsonObject.TryGetPropertyValue("lastSequenceId", out var lastSequenceIdNode))
        {
            lastSequenceId = lastSequenceIdNode.GetValue<long>();
        }

        if (jsonObject.TryGetPropertyValue("lastId", out var lastIdNode))
        {
            lastId = lastIdNode.GetValue<string>();
        }

        return (lastId, lastSequenceId);
    }

    [HttpPost]
    [Route("/api/members:batch-generate")]
    public async Task<ActionResult> BatchGenerate()
    {
        await using var dbContext = await this._memberDbContextFactory.CreateDbContextAsync();
        for (var i = 0; i < 1000; i++)
        {
            var now = DateTimeOffset.UtcNow;
            var member = new MemberDataEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = Faker.Name.FullName(),
                Age = DateTime.Now.Year - Faker.Date.Birthday().Year,
                Email = Faker.User.Email(),
                Phone = Faker.Phone.GetPhoneNumber(),
                Address = Faker.Address.SecondaryAddress(),
                CreatedAt = now,
                CreatedBy = "sys",
                UpdatedAt = now,
                UpdatedBy = "sys",
            };

            dbContext.Members.Add(member);
        }

        var count = await dbContext.SaveChangesAsync();

        return this.NoContent();
    }
}