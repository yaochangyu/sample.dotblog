using Lab.Snapshot.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lab.Snapshot.Test;

[TestClass]
public class UnitTest1
{
    private IServiceProvider _serviceProvider;

    IDbContextFactory<MemberDbContext> DbContextFactory =>
        _serviceProvider.GetService<IDbContextFactory<MemberDbContext>>();

    [TestInitialize]
    public void TestInitialize()
    {
        Console.WriteLine("TestInitialize");
        if (this._serviceProvider == null)
        {
            var services = new ServiceCollection();
            ServiceConfiguration.ConfigDb(services);
            this._serviceProvider = services.BuildServiceProvider();
        }

        this.CleanAllRecord(this.DbContextFactory.CreateDbContext());
    }

    [TestCleanup]
    public void TestCleanup()
    {
        Console.WriteLine("TestCleanup");
        this.CleanAllRecord(this.DbContextFactory.CreateDbContext());
    }

    /// <summary>
    /// 刪除資料庫所有的資料
    /// </summary>
    /// <param name="dbContext"></param>
    private void CleanAllRecord(DbContext dbContext)
    {
        dbContext.Database.ExecuteSqlRaw(NpgsqlGenerateScript.ClearAllRecord());
    }

    [TestMethod]
    public async Task 更新資料產生差異快照()
    {
        await this.InsertAsync();

        var now = TestAssistant.Now;
        var userId = TestAssistant.UserId;
        var search = "[{\"Id\": \"yao\"}]";

        await using var dbContext = await this.DbContextFactory.CreateDbContextAsync();

        // var member = dbContext.Members
        //     .Where(s => EF.Functions.JsonContains(s.Accounts, search))
        //     .AsNoTracking()
        //     .FirstOrDefault();
        var queryable = from member in dbContext.Members
                        join snapshot in dbContext.Snapshots
                            on
                            new
                            {
                                member.Id,
                                member.Version
                            }
                            equals
                            new
                            {
                                snapshot.Id,
                                snapshot.Version
                            }
                        where EF.Functions.JsonContains(member.Accounts, search)
                        select new { member, snapshot };
        var data = await queryable.AsNoTracking().FirstOrDefaultAsync();
        var profile = new Profile()
        {
            Age = 20
        };
    }

    private async Task<int> InsertAsync()
    {
        await using var dbContext = await this.DbContextFactory.CreateDbContextAsync();

        var now = TestAssistant.Now;
        var userId = TestAssistant.UserId;

        var member = new MemberDataEntity
        {
            Id = "1",
            Profile = new Profile
            {
                Age = 18,
                Name = "yao-chang"
            },
            Accounts = new List<Account>()
            {
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                }
            },
            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId,
            Version = 1
        };
        dbContext.Members.Add(member);

        dbContext.Snapshots.Add(new SnapshotDataEntity
        {
            Id = member.Id,
            Type = typeof(MemberDataEntity).ToString(),
            Data = member.ToDictionary(),
            CreatedAt = now,
            CreatedBy = userId,
            UpdatedAt = now,
            UpdatedBy = userId,
            Version = member.Version
        });
        return await dbContext.SaveChangesAsync();
    }
}