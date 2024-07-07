using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.Xunit;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FluentAssertions;
using Gherkin.Ast;
using Json.Path;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using Xunit;

namespace Lab.JsonPathForTestCase.Test;

[Binding]
public class BaseStep : Steps
{
    class Member
    {
        public int Id { get; set; }

        public int Age { get; set; }

        public DateTimeOffset Birthday { get; set; }

        public FullName FullName { get; set; }
    }

    class FullName
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
    }

    private const string StringEquals = "字串等於";
    private const string StringMatch = "字串匹配";
    private const string NumberEquals = "數值等於";
    private const string BoolEquals = "布林值等於";
    private const string JsonEquals = "Json等於";
    private const string DateTimeEquals = "時間等於";

    private const string OperationTypes = StringEquals
                                          + "|" + StringMatch
                                          + "|" + NumberEquals
                                          + "|" + BoolEquals
                                          + "|" + JsonEquals
                                          + "|" + DateTimeEquals;

    [Given(@"資料庫的 Member 資料表已經存在")]
    public void Given資料庫的Member資料表已經存在(Table table)
    {
        var members = table.CreateSet<Member>();
        this.ScenarioContext.Set(members);
    }

    [Then(@"預期資料庫的 Member 資料表應該有")]
    public void Then預期資料庫的Member資料表應該有(Table table)
    {
        var actual = this.ScenarioContext.Get<IEnumerable<Member>>();
        table.CompareToSet(actual);
    }

    [Then(@"預期回傳內容中路徑 ""(.*)"" 的(" + OperationTypes + @") ""(.*)""")]
    public void Then預期回傳內容中路徑的字串等於(string selector, string operationType, string expected)
    {
        var data = this.ScenarioContext.Get<Member>();
        var json = JsonSerializer.Serialize(data);
        ContentShouldBe(json, selector, operationType, expected);
    }

    [When(@"調用端發送 ""(.*)"" 請求至 ""(.*)""")]
    public void When調用端發送請求至(string httpMethod, string url)
    {
        var data = new Member
        {
            Id = 1,
            Age = 18,
            Birthday = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
            FullName = new FullName
            {
                FirstName = "John",
                LastName = "Doe"
            }
        };
        this.ScenarioContext.Set(data);
    }

    private static void ContentShouldBe(string content, string selectPath, string operationType, string expected)
    {
        var srcInstance = JsonNode.Parse(content);
        var jsonPath = JsonPath.Parse(selectPath);
        switch (operationType)
        {
            case StringEquals:
            {
                var actual = jsonPath.Evaluate(srcInstance).Matches.FirstOrDefault()?.Value?.GetValue<string>();
                var errorReason =
                    $"{nameof(operationType)}: [{operationType}], {nameof(selectPath)}: [{selectPath}], {nameof(expected)}: [{expected}], {nameof(actual)}: [{actual}]";
                (actual ?? string.Empty).Should().Be(expected, errorReason);
                break;
            }
            case StringMatch:
            {
                var actual = jsonPath.Evaluate(srcInstance).Matches.FirstOrDefault()?.Value?.GetValue<string>();
                var errorReason =
                    $"{nameof(operationType)}: [{operationType}], {nameof(selectPath)}: [{selectPath}], {nameof(expected)}: [{expected}], {nameof(actual)}: [{actual}]";
                new Regex(expected).IsMatch(actual ?? string.Empty).Should().BeTrue(errorReason);
                break;
            }
            case NumberEquals:
            {
                var actual = jsonPath.Evaluate(srcInstance).Matches.FirstOrDefault()?.Value?.GetValue<int>();
                var errorReason =
                    $"{nameof(operationType)}: [{operationType}], {nameof(selectPath)}: [{selectPath}], {nameof(expected)}: [{expected}], {nameof(actual)}: [{actual}]";
                actual.Should().Be(int.Parse(expected), errorReason);
                break;
            }
            case BoolEquals:
            {
                var actual = jsonPath.Evaluate(srcInstance).Matches.FirstOrDefault()?.Value?.GetValue<bool>();
                var errorReason =
                    $"{nameof(operationType)}: [{operationType}], {nameof(selectPath)}: [{selectPath}], {nameof(expected)}: [{expected}], {nameof(actual)}: [{actual}]";
                actual.Should().Be(bool.Parse(expected), errorReason);
                break;
            }
            case DateTimeEquals:
            {
                var expect = DateTimeOffset.Parse(expected);
                var actual = jsonPath.Evaluate(srcInstance).Matches.FirstOrDefault()
                        ?.Value
                        ?.GetValue<DateTimeOffset>()
                    ;
                var errorReason =
                    $"{nameof(operationType)}: [{operationType}], {nameof(selectPath)}: [{selectPath}], {nameof(expected)}: [{expect}], {nameof(actual)}: [{actual}]";
                actual.Should().Be(expect, errorReason);
                break;
            }
            case JsonEquals:
            {
                var actual = jsonPath.Evaluate(srcInstance).Matches.FirstOrDefault()?.Value;
                var expect = string.IsNullOrWhiteSpace(expected) ? null : JsonNode.Parse(expected);
                var diff = actual.Diff(expect);
                var errorReason =
                    $"{nameof(operationType)}: [{operationType}], {nameof(selectPath)}: [{selectPath}], {nameof(expected)}: [{expected}], {nameof(actual)}: [{actual?.ToJsonString()}], diff: [{diff?.ToJsonString()}]";
                actual.DeepEquals(expect).Should().BeTrue(errorReason);
                break;
            }
        }
    }

    [Then(@"預期得到回傳 Member 結果為")]
    public void Then預期得到回傳Member結果為(Table table)
    {
        var actual = this.ScenarioContext.Get<Member>();
        table.CompareToInstance(actual);
    }

    [Then(@"預期得到回傳 Member\.FullName 結果為")]
    public void Then預期得到回傳MemberFullName結果為(Table table)
    {
        var actual = this.ScenarioContext.Get<Member>().FullName;
        table.CompareToInstance(actual);
    }

    [Then(@"預期回傳內容為")]
    public void Then預期回傳內容為(string expected)
    {
        var data = this.ScenarioContext.Get<Member>();
        var actual = JsonSerializer.Serialize(data);
        JsonAssert.Equal(expected, actual,true);
    }
}