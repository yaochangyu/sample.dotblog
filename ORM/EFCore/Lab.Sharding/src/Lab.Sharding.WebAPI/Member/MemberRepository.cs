﻿using System.Text.Json;
using Lab.Sharding.DB;
using Lab.Sharding.Infrastructure;
using Lab.Sharding.Infrastructure.TraceContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Lab.Sharding.WebAPI.Member;

public class MemberRepository(
	ILogger<MemberRepository> logger,
	IContextGetter<TraceContext?> contextGetter,
	IDynamicDbContextFactory<DynamicMemberDbContext> dynamicDbContextFactory,
	TimeProvider timeProvider,
	IUuidProvider uuidProvider,
	IDistributedCache cache,
	JsonSerializerOptions jsonSerializerOptions)
{
	public async Task<PaginatedList<GetMemberResponse>>
		GetMembersAsync(int pageIndex, int pageSize, bool noCache = false, CancellationToken cancel = default)
	{
		var traceContext = contextGetter.Get();
		var userId = traceContext.UserId;
		PaginatedList<GetMemberResponse> result;
		var key = nameof(CacheKeys.MemberData);
		string cachedData;
		if (noCache == false) // 如果有快取，就從快取撈資料
		{
			cachedData = await cache.GetStringAsync(key, cancel);
			if (cachedData != null)
			{
				result = JsonSerializer.Deserialize<PaginatedList<GetMemberResponse>>(
					cachedData, jsonSerializerOptions);
				return result;
			}
		}

		var serverName = nameof(ServerNames.Server01);
		var dbName = nameof(DbNames.MemberDb01);

		await using var dbContext = dynamicDbContextFactory.CreateDbContext(serverName,
																			dbName,
																			"01");
		var selector = dbContext.Members
			.Select(p => new GetMemberResponse { Id = p.Id, Name = p.Name, Age = p.Age, Email = p.Email })
			.AsNoTracking();

		var totalCount = selector.Count();
		var paging = selector.OrderBy(p => p.Id)
			.Skip(pageIndex * pageSize)
			.Take(pageSize);
		var data = await paging
			.TagWith($"{nameof(MemberRepository)}.{nameof(this.GetMembersAsync)}")
			.ToListAsync(cancel);
		result = new PaginatedList<GetMemberResponse>(data, pageIndex, pageSize, totalCount);
		cachedData = JsonSerializer.Serialize(result, jsonSerializerOptions);
		cache.SetStringAsync(key, cachedData,
							 new DistributedCacheEntryOptions
							 {
								 AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) //最好從組態設定讀取
							 }, cancel);

		return result;
	}
}