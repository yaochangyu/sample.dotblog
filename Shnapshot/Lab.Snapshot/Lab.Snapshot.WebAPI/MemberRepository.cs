using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs;
using System.Text.Json.Nodes;
using AutoMapper;
using Lab.Snapshot.DB;
using Lab.Snapshot.WebAPI.ServiceModels;
using Microsoft.EntityFrameworkCore;

namespace Lab.Snapshot.WebAPI;

public class MemberRepository
{
    private readonly IDbContextFactory<MemberDbContext> _memberDbContextFactory;
    private readonly IMapper _mapper;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly IContextGetter<AuthContext> _contextGetter;

    public MemberRepository(IDbContextFactory<MemberDbContext> memberDbContextFactory,
        IMapper mapper,
        JsonSerializerOptions jsonSerializerOptions,
        IContextGetter<AuthContext> contextGetter)
    {
        this._memberDbContextFactory = memberDbContextFactory;
        this._mapper = mapper;
        this._jsonSerializerOptions = jsonSerializerOptions;
        this._contextGetter = contextGetter;
    }

    public async Task<(Failure Failure, bool Data)> InsertMemberAsync(InsertMemberRequest request)
    {
        var authContext = this._contextGetter.Get();
        var search = $"[{{\"Id\": \"{request.Account.Id}\"}}]";

        await using var dbContext = await this._memberDbContextFactory.CreateDbContextAsync();
        var accountExist = dbContext.Members.AsNoTracking().Any(p => EF.Functions.JsonContains(p.Accounts, search));
        if (accountExist)
        {
            return (new Failure(FailureCode.MemberExist, $"Member({request.Account.Id}) exist"), false);
        }

        var member = new MemberDataEntity
        {
            Id = Guid.NewGuid().ToString(),
            Profile = this._mapper.Map<DB.Profile>(request.Profile),
            Accounts = new List<DB.Account>
            {
                this._mapper.Map<DB.Account>(request.Account)
            },
            CreatedAt = authContext.Now,
            CreatedBy = authContext.UserId,
            UpdatedAt = authContext.Now,
            UpdatedBy = authContext.UserId,
            Version = 1
        };
        var snapshot = new SnapshotDataEntity
        {
            Id = member.Id,
            DataType = typeof(MemberDataEntity).ToString(),
            Data = JsonNode.Parse(JsonSerializer.Serialize(member, this._jsonSerializerOptions)),
            DataFormat = DataFormat.Full.ToString(),
            CreatedAt = authContext.Now,
            CreatedBy = authContext.UserId,
            Version = member.Version
        };
        await dbContext.Members.AddAsync(member);
        await dbContext.Snapshots.AddAsync(snapshot);
        await dbContext.SaveChangesAsync();
        return (null, true);
    }

    public async Task<(Failure Failure, bool Data)> BindMemberAsync(string currentAccount,
        UpdateMemberRequest request)
    {
        var authContext = this._contextGetter.Get();
        var search = $"[{{\"Id\": \"{currentAccount}\"}}]";

        await using var dbContext = await this._memberDbContextFactory.CreateDbContextAsync();
        var oldMember = await dbContext.Members.FirstOrDefaultAsync(p => EF.Functions.JsonContains(p.Accounts, search));
        if (oldMember == null)
        {
            return (new Failure(FailureCode.MemberNotExist, $"Member({currentAccount}) not exist"), false);
        }

        var newMember = oldMember with
        {
            Accounts = this._mapper.Map<List<DB.Account>>(oldMember.Accounts),
            Profile = this._mapper.Map<DB.Profile>(oldMember.Profile)
        };

        newMember.Profile = this._mapper.Map<DB.Profile>(request.Profile);

        // 帳號不存在才加入
        foreach (var srcAccount in request.Accounts)
        {
            var accountExist = false;
            foreach (var destAccount in newMember.Accounts)
            {
                if (srcAccount.Id == destAccount.Id)
                {
                    accountExist = true;
                    break;
                }
            }

            if (accountExist == false)
            {
                var map = this._mapper.Map<DB.Account>(srcAccount);
                newMember.Accounts.Add(map);
            }
        }

        var oldData = JsonSerializer.Serialize(oldMember, this._jsonSerializerOptions);
        var newData = JsonSerializer.Serialize(newMember, this._jsonSerializerOptions);
        var diff = JsonDiffPatcher.Diff(oldData, newData,
            new JsonDiffOptions
            {
                JsonElementComparison = JsonElementComparison.Semantic,
            });

        if (diff == null)
        {
            return (null, true);
        }

        // 不納入比對的欄位，最後再更新成最新狀態
        newMember.UpdatedAt = authContext.Now;
        newMember.UpdatedBy = authContext.UserId;
        newMember.Version = oldMember.Version + 1;

        oldData = JsonSerializer.Serialize(oldMember, this._jsonSerializerOptions);
        newData = JsonSerializer.Serialize(newMember, this._jsonSerializerOptions);
        diff = JsonDiffPatcher.Diff(oldData, newData,
            new JsonDiffOptions
            {
                JsonElementComparison = JsonElementComparison.Semantic,
            });

        var snapshot = new SnapshotDataEntity
        {
            Id = newMember.Id,
            DataType = typeof(MemberDataEntity).ToString(),
            Data = diff,
            DataFormat = DataFormat.Diff.ToString(),
            CreatedAt = newMember.UpdatedAt,
            CreatedBy = newMember.UpdatedBy,
            Version = newMember.Version
        };

        var entry = dbContext.Members.Entry(oldMember);
        entry.CurrentValues.SetValues(newMember);
        await dbContext.Snapshots.AddAsync(snapshot);
        await dbContext.SaveChangesAsync();
        return (null, true);
    }

    public async Task<MemberResponse> GetMemberAsync(string account, int? version)
    {
        var search = $"[{{\"Id\": \"{account}\"}}]";

        await using var dbContext = await this._memberDbContextFactory.CreateDbContextAsync();
        if (version.HasValue == false)
        {
            var queryable = from member in dbContext.Members
                            where EF.Functions.JsonContains(member.Accounts, search)
                            select new { member };
            var data = await queryable.AsNoTracking().FirstOrDefaultAsync();
            if (data == null)
            {
                return null;
            }

            return this._mapper.Map<MemberResponse>(data.member);
        }

        // 處理快照
        var entities = dbContext.Snapshots
                .Where(p => p.Version <= version)
                .OrderBy(p => p.Version)
            ;
        var snapshots = await entities.AsNoTracking().ToListAsync();
        JsonNode finial = null;
        foreach (var snapshot in snapshots)
        {
            if (snapshot.Version == 1)
            {
                finial = snapshot.Data;
                continue;
            }

            JsonDiffPatcher.Patch(ref finial, snapshot.Data);
        }

        // search account in JsonNode
        var accounts = finial!["accounts"]!.AsArray();
        foreach (var item in accounts)
        {
            var id = item["id"]!.GetValue<string>();
            if (id == account)
            {
                var result = finial.Deserialize<MemberResponse>(this._jsonSerializerOptions);
                return result;
            }
        }

        return null;
    }
}