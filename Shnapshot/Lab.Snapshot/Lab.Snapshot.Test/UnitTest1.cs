using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Diffs.Formatters;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentAssertions;
using JsonDiffPatchDotNet;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using Lab.Snapshot.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonConverter = System.Text.Json.Serialization.JsonConverter;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Lab.Snapshot.Test;

[TestClass]
public class UnitTest1
{
    private IServiceProvider _serviceProvider;

    IDbContextFactory<MemberDbContext> DbContextFactory =>
        _serviceProvider.GetService<IDbContextFactory<MemberDbContext>>();

    static JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

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
        // dbContext.Database.ExecuteSqlRaw(NpgsqlGenerateScript.ClearAllRecord());
    }

    [TestMethod]
    public async Task Diff()
    {
        var left = """
                   {
                     "id": 100,
                     "revision": 5,
                     "items": [
                       "car",
                       "bus"
                     ],
                     "tagline": "I can't do it. This text is too long for me to handle! Please help me JsonDiffPatch!",
                     "author": "wbish"
                   }
                   """;

        var right = """
                    {
                      "id": 100,
                      "revision": 6,
                      "items": [
                        "bike",
                        "bus",
                        "car"
                      ],
                      "tagline": "I can do it. This text is not too long. Thanks JsonDiffPatch!",
                      "author": {
                        "first": "w",
                        "last": "bish"
                      }
                    }
                    """;

        var jdp = new JsonDiffPatch();
        var output = jdp.Diff(left, right);
        var formatter = new JsonDeltaFormatter();
        var operations = formatter.Format(output);
        var patch = new JsonDiffPatch().Diff(left, right);
    }

    [TestMethod]
    public async Task Patch()
    {
        var left = JObject.Parse("{ \"name\": \"Justin\" }");
        var right = JObject.Parse("{ \"name\" : \"John\", \"age\": 34 }");
        var patch = new JsonDiffPatch().Diff(left, right);
        var formatter = new JsonDeltaFormatter();
        var operations = formatter.Format(patch);
        var jToken = new JsonDiffPatch().Patch(left, patch);
    }

    [TestMethod]
    public async Task Newtonsoft_DiffPatch()
    {
        var oldMember = new MemberDataEntity
        {
            Id = "1",
            Profile = new Profile
            {
                Age = 19,
                Name = "yao-chang"
            },
            Accounts = new List<Account>
            {
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
            Version = 1
        };

        var newMember = new MemberDataEntity
        {
            Id = "1",
            Profile = new Profile
            {
                Age = 19,
                Name = "小章"
            },
            Accounts = new List<Account>
            {
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                },
                new()
                {
                    Id = "yao1",
                    Type = "VIP1"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
            Version = 2
        };

        var oldData = JsonConvert.SerializeObject(oldMember);
        var newData = JsonConvert.SerializeObject(newMember);
        var diff = new JsonDiffPatch().Diff(oldData, newData);
        var patchData = new JsonDiffPatch().Patch(oldData, diff);

        var actual = JsonConvert.DeserializeObject<MemberDataEntity>(patchData);
        actual.Should().BeEquivalentTo(newMember);
    }

    [TestMethod]
    public async Task SystemTextJson_DiffPatch()
    {
        var oldMember = new MemberDataEntity
        {
            Id = "1",
            Profile = new Profile
            {
                Age = 19,
                Name = "yao-chang"
            },
            Accounts = new List<Account>
            {
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
            Version = 1
        };

        var newMember = new MemberDataEntity
        {
            Id = "1",
            Profile = new Profile
            {
                Age = 19,
                Name = "小章"
            },
            Accounts = new List<Account>
            {
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                },
                new()
                {
                    Id = "yao1",
                    Type = "VIP1"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
            Version = 2
        };

        var oldData = JsonSerializer.Serialize(oldMember, Options);
        var newData = JsonSerializer.Serialize(newMember, Options);

        var diff = JsonDiffPatcher.Diff(oldData, newData, new JsonDiffOptions
        {
            JsonElementComparison = JsonElementComparison.Semantic
        });
        var diff1 = JsonDiffPatcher.Diff(oldData, newData);
        var diff2 = JsonDiffPatcher.Diff(oldData, newData, new JsonPatchDeltaFormatter(), new JsonDiffOptions
        {
            JsonElementComparison = JsonElementComparison.Semantic
        });
        var result = JsonNode.Parse(oldData);
        JsonDiffPatcher.Patch(ref result, diff);
        var actual = result.Deserialize<MemberDataEntity>(Options);
        actual.Should().BeEquivalentTo(newMember);
    }

    [TestMethod]
    public async Task SystemTextJson_DiffPatch_1()
    {
        var original = """
                      {
                          "id": "1",
                          "profile": {
                              "age": 18,
                              "name": "yao-chang"
                          },
                          "version": 1,
                          "accounts": [
                              {
                                  "id": "yao",
                                  "type": "VIP"
                              }
                          ],
                          "createdAt": "2023-11-01T16:25:29.5966704+00:00",
                          "createdBy": "@@TestUser@@",
                          "updatedAt": "2023-11-01T16:25:29.5966704+00:00",
                          "updatedBy": "@@TestUser@@"
                      }
                      """;
        var diffText = """
                      {
                          "profile": {
                              "age": [
                                  18,
                                  20
                              ],
                              "name": [
                                  "yao-chang",
                                  "jordan"
                              ]
                          },
                          "version": [
                              1,
                              0
                          ],
                          "accounts": {
                              "1": [
                                  {
                                      "id": "yao1",
                                      "type": "VIP1"
                                  }
                              ],
                              "2": [
                                  {
                                      "id": "jordan1",
                                      "type": "VVVIP"
                                  }
                              ],
                              "_t": "a"
                          },
                          "createdAt": [
                              "2023-11-01T16:25:29.59667+00:00",
                              "2023-11-01T16:25:30.2735717+00:00"
                          ],
                          "createdBy": [
                              "@@TestUser@@",
                              "test"
                          ],
                          "updatedAt": [
                              "2023-11-01T16:25:29.59667+00:00",
                              "2023-11-01T16:25:30.2735718+00:00"
                          ],
                          "updatedBy": [
                              "@@TestUser@@",
                              "test"
                          ]
                      }
                      """;
        var result = JsonNode.Parse(original);
        var diff = JsonNode.Parse(diffText);
        JsonDiffPatcher.Patch(ref result, diff);
        var actual = result.Deserialize<MemberDataEntity>(Options);
    }

    [TestMethod]
    public async Task SystemTextJson_RestoreInMemory()
    {
        var oldMember = new MemberDataEntity
        {
            Id = "1",
            Profile = new Profile
            {
                Age = 19,
                Name = "小章"
            },
            Accounts = new List<Account>
            {
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                },
                new()
                {
                    Id = "yao1",
                    Type = "VIP1"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
            Version = 2
        };

        var newMember = new MemberDataEntity
        {
            Id = "1",
            Profile = new Profile
            {
                Age = 19,
                Name = "小章"
            },
            Accounts = new List<Account>
            {
                new()
                {
                    Id = "yao1",
                    Type = "VIP1"
                },
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                },
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
            Version = 2
        };

        var oldData = JsonSerializer.Serialize(oldMember, Options);
        var newData = JsonSerializer.Serialize(newMember, Options);

        var diff = JsonDiffPatcher.Diff(oldData, newData, new JsonDiffOptions
        {
            JsonElementComparison = JsonElementComparison.Semantic
        });
        var diff1 = JsonDiffPatcher.Diff(oldData, newData);
        var diff2 = JsonDiffPatcher.Diff(oldData, newData, new JsonPatchDeltaFormatter(), new JsonDiffOptions
        {
            JsonElementComparison = JsonElementComparison.Semantic
        });
        var result = JsonNode.Parse(oldData);
        JsonDiffPatcher.Patch(ref result, diff);
        var actual = result.Deserialize<MemberDataEntity>(Options);
        actual.Should().BeEquivalentTo(newMember);
    }

    [TestMethod]
    public async Task SystemTextJson_RestoreInDb()
    {
        await this.InsertOrGetAsync();
        await this.Update(GenerateMember(20, "jordan", new Account
        {
            Id = "jordan1",
            Type = "VVVIP"
        }));
        await this.Update(GenerateMember(18, "yao", null));
        await this.Update(GenerateMember(32, "yao-chang", null));

        await using var dbContext = await this.DbContextFactory.CreateDbContextAsync();
        var snapshots = await dbContext.Snapshots
            .Where(p => p.Id == "1")
            .OrderBy(p => p.Version)
            .AsNoTracking()
            .ToListAsync();

        JsonNode full = null;
        foreach (var snapshot in snapshots)
        {
            if (snapshot.Version==1)
            {
                full = snapshot.Data;
                continue;
            }
            JsonDiffPatcher.Patch(ref full, snapshot.Data);

            // var diff = new JsonDiffPatch().Diff(oldData, newData);
            // var patchData = new JsonDiffPatch().Patch(oldData, diff);
        }

        var jsonString = full.ToJsonString();
    }

    private static string ToJsonString(JsonDocument doc)
    {
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        doc.WriteTo(writer);
        writer.Flush();
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private async Task Update(MemberDataEntity newMember)
    {
        await using var dbContext = await this.DbContextFactory.CreateDbContextAsync();

        // 取得資料 sub query
        var queryable = from member in dbContext.Members
                        where member.Id == "1"
                        select new
                        {
                            member,
                            snapshots = dbContext.Snapshots
                                .Where(p => p.Id == "1")
                                .ToList()
                        };
        var data = await queryable.FirstOrDefaultAsync();
        var oldMember = data.member;

        // 比對差異
        var oldData = JsonSerializer.Serialize(oldMember, Options);
        var newData = JsonSerializer.Serialize(newMember, Options);
        var diff = JsonDiffPatcher.Diff(oldData, newData);

        // var oldData = JsonConvert.SerializeObject(oldMember);
        // var newData = JsonConvert.SerializeObject(newMember);
        // var jsonDiffPatch = new JsonDiffPatch();
        // var diff = jsonDiffPatch.Diff(oldData, newData);
        // if (string.IsNullOrWhiteSpace(diff))
        // {
        //     return;
        // }

        var newVersion = oldMember.Version + 1;
        dbContext.Entry(oldMember).CurrentValues.SetValues(newMember);
        oldMember.Version = newVersion;
        var entity = new SnapshotDataEntity
        {
            Id = oldMember.Id,
            Data = diff,
            DataType = typeof(MemberDataEntity).ToString(),
            DataFormat = DataFormat.Diff.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            Version = newVersion
        };
        dbContext.Snapshots.Add(entity);

        var changes = await dbContext.SaveChangesAsync();
    }

    private static MemberDataEntity GenerateMember(int age, string name, Account account)
    {
        var newMember = new MemberDataEntity()
        {
            Id = "1",
            Profile = new Profile
            {
                Age = age,
                Name = name
            },
            Accounts = new List<Account>
            {
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                },
                new()
                {
                    Id = "yao1",
                    Type = "VIP1"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
        };
        if (account != null)
        {
            newMember.Accounts.Add(account);
        }

        return newMember;
    }

    [TestMethod]
    public async Task 更新資料產生差異快照()
    {
        var oldMember = await this.InsertOrGetAsync();
        var newMember = new MemberDataEntity()
        {
            Id = "1",
            Profile = new Profile
            {
                Age = 19,
                Name = "小章"
            },
            Accounts = new List<Account>
            {
                new()
                {
                    Id = "yao",
                    Type = "VIP"
                },
                new()
                {
                    Id = "yao1",
                    Type = "VIP1"
                }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test",
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test",
            Version = 2
        };

        var search = "[{\"Id\": \"yao\"}]";

        await using var dbContext = await this.DbContextFactory.CreateDbContextAsync();

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
                        where member.Id == "1"
                        select new { member, snapshot };

        var data = await queryable.FirstOrDefaultAsync();
        var oldData = JsonConvert.SerializeObject(oldMember);
        var newData = JsonConvert.SerializeObject(newMember);
        var jsonDiffPatch = new JsonDiffPatch();
        var diff = jsonDiffPatch.Diff(oldData, newData);
        var formatter = new JsonDeltaFormatter();

        dbContext.Entry(data.member).CurrentValues.SetValues(newData);
        dbContext.Snapshots.Add(new SnapshotDataEntity
        {
            Id = oldMember.Id,
            Data = JsonNode.Parse(diff),
            DataType = typeof(MemberDataEntity).ToString(),
            DataFormat = "Diff",
            CreatedAt = data.snapshot.CreatedAt,
            CreatedBy = data.snapshot.CreatedBy,
            Version = newMember.Version
        });
        var changes = dbContext.SaveChanges();
    }

    [TestMethod]
    public async Task ORM查詢Jsonb()
    {
        await this.InsertOrGetAsync();

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
        data.member.Id.Should().Be("1");
    }

    private async Task<MemberDataEntity> InsertOrGetAsync()
    {
        await using var dbContext = await this.DbContextFactory.CreateDbContextAsync();

        var now = TestAssistant.Now;
        var userId = TestAssistant.UserId;
        var member = await dbContext.Members.Where(p => p.Id == "1").AsNoTracking().FirstOrDefaultAsync();
        if (member != null)
        {
            return member;
        }

        member = new MemberDataEntity
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
            Data = JsonNode.Parse(JsonSerializer.Serialize(member, Options)),
            DataType = typeof(MemberDataEntity).ToString(),
            DataFormat = "Full",
            CreatedAt = now,
            CreatedBy = userId,
            Version = member.Version
        });
        var count = await dbContext.SaveChangesAsync();
        return member;
    }
}