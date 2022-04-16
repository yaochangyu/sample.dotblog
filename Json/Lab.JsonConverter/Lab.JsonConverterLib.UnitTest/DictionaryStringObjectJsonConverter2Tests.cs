using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.JsonConverterLib.UnitTest;

[TestClass]
public class DictionaryStringObjectJsonConverter2Tests
{
    [TestMethod]
    public void JsonDocument轉Dictionary()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter2() }
        };
        var expected = CreateDictionary();
        var json = JsonSerializer.Serialize(expected);

        using var jsonDoc = json.ToJsonDocument();
        var actual = jsonDoc.To<Dictionary<string, object>>(options);
        AssertThat(actual, expected);
    }

    [TestMethod]
    public void JsonsNode轉Dictionary()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter2() }
        };
        var expected = CreateDictionary();
        var json = JsonSerializer.Serialize(expected);

        var jsonObject = json.ToJsonNode();
        var actual = jsonObject.To<Dictionary<string, object>>(options);

        AssertThat(actual, expected);
    }

    private static void AssertThat(Dictionary<string, object> actual, Dictionary<string, object> expected)
    {
        actual["model"].Should().BeEquivalentTo(expected["model"]);
        actual["decimalArray"].Should().BeEquivalentTo(expected["decimalArray"]);
        Assert.AreEqual(expected["dateTimeOffset"], actual["dateTimeOffset"]);
        Assert.AreEqual(expected["decimal"], actual["decimal"]);
        Assert.AreEqual(expected["guid"], actual["guid"]);
        Assert.AreEqual(expected["string"], actual["string"]);
    }

    [TestMethod]
    public void Memory轉Dictionary()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var expected = CreateDictionary();

        var json = JsonSerializer.Serialize(expected, options);
        var jsonMemory = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonMemory, options);

        AssertThat(actual, expected);
    }

    [TestMethod]
    public void 字串轉Dictionary()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var expected = CreateDictionary();

        var json = JsonSerializer.Serialize(expected, options);
        var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

        AssertThat(actual, expected);
    }

    [TestMethod]
    public void 字串轉Dictionary_失敗()
    {
        var expected = new Dictionary<string, object>
        {
            ["i"] = 255,
            ["s"] = "字串",
            ["d"] = new DateTime(1900, 1, 1),
            ["a"] = new[] { 1, 2 },
            ["o"] = new { Prop = 123 }
        };
        var json = JsonSerializer.Serialize(expected);

        var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        Assert.AreNotEqual(expected["i"], actual["i"]);
        Assert.AreNotEqual(expected["s"], actual["s"]);

        // 反序列化之後得到 JsonElement Type，必須要要透過其他手段才能取得真實的值
        Assert.AreEqual("JsonElement", actual["s"].GetType().Name);
        Assert.AreEqual(expected["i"], ((JsonElement)actual["i"]).GetInt32());
        Assert.AreEqual(expected["s"], ((JsonElement)actual["s"]).GetString());
    }

    private static Dictionary<string, object> CreateDictionary()
    {
        var expected = new Dictionary<string, object>
        {
            ["model"] = new Dictionary<string, object>
            {
                { "Age", 19 },
                { "Name", "小章" }
            },
            ["decimalArray"] = new List<decimal> { 1, (decimal)2.1 },
            ["dateTimeOffset"] = DateTimeOffset.Now,
            ["decimal"] = (decimal)3.1416,
            ["guid"] = Guid.NewGuid(),
            ["string"] = "字串",
        };
        return expected;
    }

    private class Model
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}