using System.Text.Json;
using FluentAssertions;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Lab.SpecflowCreateAndCompareJson;

[Binding]
public class BaseStep
{
    [Given(@"已準備 Member 資料\(錯誤\)")]
    public void Given已準備Member資料錯誤(Table table)
    {
        var members = table.CreateSet(row =>
        {
            var member = new Member
            {
                Id = null,
                Name = null,
                IpData = null,
                Orders = null
            };

            return member;
        });
    }

    [Given(@"已準備 Member 資料\(正確\)")]
    public void Given已準備Member資料正確(Table table)
    {
        var members = new List<Member>();
        foreach (var row in table.Rows)
        {
            var member = new Member();
            foreach (var header in table.Header)
            {
                switch (header)
                {
                    case nameof(Member.Id):
                        member.Id = row[header];
                        break;
                    case nameof(Member.Age):
                        member.Age = int.Parse(row[header]);
                        break;
                    case nameof(Member.State):
                        member.State = Enum.Parse<State>(row[header]);
                        break;
                    case nameof(Member.Name):
                        member.Name = TryGetName(row);
                        break;
                    case nameof(Member.IpData):
                        member.IpData = TryGetIpData(row);
                        break;
                    case nameof(Member.Orders):
                        member.Orders = TryGetOrders(row);
                        break;
                }
            }

            members.Add(member);
        }
    }

    private static Name? TryGetName(TableRow row)
    {
        var data = row.TryGetValue(nameof(Member.Name), out var name)
            ? JsonSerializer.Deserialize<Name>(name)
            : null;
        return data;
    }

    private static List<Order>? TryGetOrders(TableRow row)
    {
        var data = row.TryGetValue(nameof(Member.Orders), out var orders)
            ? JsonSerializer.Deserialize<List<Order>>(orders)
            : new List<Order>();
        return data;
    }

    private static List<string>? TryGetIpData(TableRow row)
    {
        var data = row.TryGetValue(nameof(Member.IpData), out var ip)
            ? JsonSerializer.Deserialize<List<string>>(ip)
            : new List<string>();
        return data;
    }

    [Given(@"已準備 Member 資料\(擴充方法\)")]
    public void Given已準備Member資料擴充方法(Table table)
    {
        var members = table.CreateJsonSet<Member>();
    }

    [Then(@"預期得到 Member 資料\(錯誤\)")]
    public void Then預期得到Member資料錯誤(Table table)
    {
        var actual = CreateActualMembers();
        table.CompareToSet(actual);
    }

    [Then(@"預期得到 Member 資料\(正確\)")]
    public void Then預期得到Member資料正確(Table table)
    {
        var actual = CreateActualMembers();
        var expected = table.CreateJsonSet<Member>();
        var header = table.Header.ToHashSet();

        // actual.Should().BeEquivalentTo(expected, options => options
        //     .Including(x => x.Id)
        //     .Including(x => x.Age)
        //     .Including(x => x.Name)
        //     .Including(x => x.State)
        //     .Including(x => x.IpData)
        //     .Including(x => x.Orders)
        // );

        actual.Should().BeEquivalentTo(expected, options =>
        {
            options.Including(info => header.Contains(info.Name));
            if (header.Contains(nameof(Member.Name)))
            {
                options.Including(info => info.Name);
            }
            return options;
        });

        // actual.Should()
        //     .BeEquivalentTo(expected);
    }

    private static List<Member> CreateActualMembers()
    {
        List<Member> actual =
        [
            new Member
            {
                Id = "1",
                Age = 18,
                Name = new Name
                {
                    FirstName = "yaochang",
                    LastName = "yu"
                },
                State = State.Active,
                IpData = ["192.168.0.1", "192.168.0.2"],
                Orders = [new Order { Id = "123" }]
            }
        ];
        return actual;
    }
}