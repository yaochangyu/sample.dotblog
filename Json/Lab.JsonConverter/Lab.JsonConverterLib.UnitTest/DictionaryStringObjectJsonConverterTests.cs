using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.JsonConverterLib.UnitTest;

[TestClass]
public class DictionaryStringObjectJsonConverterTests
{
    [TestMethod]
    public void JsonDocument轉Dictionary()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var expected = new Dictionary<string, object>
        {
            ["anonymousType"] = new { Prop = 123 },
            ["model"] = new Model { Age = 19, Name = "小章" },
            ["null"] = null!,
            ["dateTimeOffset"] = DateTimeOffset.Now,
            ["long"] = (long)255,
            ["decimal"] = (decimal)3.1416,
            ["guid"] = Guid.NewGuid(),
            ["string"] = "字串",
            ["decimalArray"] = new[] { 1, (decimal)2.1 },
        };
        var json = JsonSerializer.Serialize(expected);

        using var jsonDoc = json.ToJsonDocument();
        var actual = jsonDoc.To<Dictionary<string, object>>(options);

        Assert.AreEqual(expected["dateTimeOffset"], actual["dateTimeOffset"]);
        Assert.AreEqual(expected["string"], actual["string"]);
        Assert.AreEqual(expected["long"], actual["long"]);
        Assert.AreEqual(expected["decimal"], actual["decimal"]);
        Assert.AreEqual(expected["null"], actual["null"]);

        AssertAnonymousType(actual["anonymousType"] as Dictionary<string, object>);
        AssertDecimalArray(actual["decimalArray"] as List<object>);
    }

    [TestMethod]
    public void JsonDocument轉Dictionary1()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var expected = new Dictionary<string, object>
        {
            ["anonymousType"] = new { Prop = 123 },
            ["model"] = new Model { Age = 19, Name = "小章" },
            ["null"] = null!,
            ["dateTimeOffset"] = DateTimeOffset.Now,
            ["long"] = (long)255,
            ["decimal"] = (decimal)3.1416,
            ["guid"] = Guid.NewGuid(),
            ["string"] = "字串",
            ["decimalArray"] = new[] { 1, (decimal)2.1 },
        };

        using var jsonDoc = expected.ToJsonDocument();
        var actual = jsonDoc.To<Dictionary<string, object>>(options);

        Assert.AreEqual(expected["dateTimeOffset"], actual["dateTimeOffset"]);
        Assert.AreEqual(expected["string"], actual["string"]);
        Assert.AreEqual(expected["long"], actual["long"]);
        Assert.AreEqual(expected["decimal"], actual["decimal"]);
        Assert.AreEqual(expected["null"], actual["null"]);

        AssertAnonymousType(actual["anonymousType"] as Dictionary<string, object>);
        AssertDecimalArray(actual["decimalArray"] as List<object>);
    }

    [TestMethod]
    public void JsonsNode轉Dictionary()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var expected = new Dictionary<string, object>
        {
            ["anonymousType"] = new { Prop = 123 },
            ["model"] = new Model { Age = 19, Name = "小章" },
            ["null"] = null!,
            ["dateTimeOffset"] = DateTimeOffset.Now,
            ["long"] = (long)255,
            ["decimal"] = (decimal)3.1416,
            ["guid"] = Guid.NewGuid(),
            ["string"] = "字串",
            ["decimalArray"] = new[] { 1, (decimal)2.1 },
        };
        var json = JsonSerializer.Serialize(expected);

        var jsonObject = json.ToJsonNode();
        var actual = jsonObject.To<Dictionary<string, object>>(options);

        Assert.AreEqual(expected["dateTimeOffset"], actual["dateTimeOffset"]);
        Assert.AreEqual(expected["string"], actual["string"]);
        Assert.AreEqual(expected["long"], actual["long"]);
        Assert.AreEqual(expected["decimal"], actual["decimal"]);
        Assert.AreEqual(expected["null"], actual["null"]);

        AssertAnonymousType(actual["anonymousType"] as Dictionary<string, object>);
        AssertDecimalArray(actual["decimalArray"] as List<object>);
    }

    [TestMethod]
    public void Memory轉Dictionary()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };

        var expected = new Dictionary<string, object>
        {
            ["anonymousType"] = new { Prop = 123 },
            ["model"] = new Model { Age = 19, Name = "小章" },
            ["null"] = null!,
            ["dateTimeOffset"] = DateTimeOffset.Now,
            ["long"] = (long)255,
            ["decimal"] = (decimal)3.1416,
            ["guid"] = Guid.NewGuid(),
            ["string"] = "字串",
            ["decimalArray"] = new[] { 1, (decimal)2.1 },
        };

        var json = JsonSerializer.Serialize(expected, options);
        var jsonMemory = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonMemory, options);

        Assert.AreEqual(expected["dateTimeOffset"], actual["dateTimeOffset"]);
        Assert.AreEqual(expected["string"], actual["string"]);
        Assert.AreEqual(expected["long"], actual["long"]);
        Assert.AreEqual(expected["decimal"], actual["decimal"]);
        Assert.AreEqual(expected["null"], actual["null"]);

        AssertAnonymousType(actual["anonymousType"] as Dictionary<string, object>);
        AssertDecimalArray(actual["decimalArray"] as List<object>);
    }

    [TestMethod]
    public void 字串轉Dictionary()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DictionaryStringObjectJsonConverter() }
        };
        var expected = new Dictionary<string, object>
        {
            ["anonymousType"] = new { Prop = 123 },
            ["model"] = new Model { Age = 19, Name = "小章" },
            ["null"] = null!,
            ["dateTimeOffset"] = DateTimeOffset.Now,
            ["long"] = (long)255,
            ["decimal"] = (decimal)3.1416,
            ["guid"] = Guid.NewGuid(),
            ["string"] = "字串",
            ["decimalArray"] = new[] { 1, (decimal)2.1 },
        };

        var json = JsonSerializer.Serialize(expected, options);
        var actual = JsonSerializer.Deserialize<Dictionary<string, object>>(json, options);

        Assert.AreEqual(expected["dateTimeOffset"], actual["dateTimeOffset"]);
        Assert.AreEqual(expected["string"], actual["string"]);
        Assert.AreEqual(expected["long"], actual["long"]);
        Assert.AreEqual(expected["decimal"], actual["decimal"]);
        Assert.AreEqual(expected["null"], actual["null"]);

        AssertAnonymousType(actual["anonymousType"] as Dictionary<string, object>);
        AssertDecimalArray(actual["decimalArray"] as List<object>);
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

    private static void AssertAnonymousType(Dictionary<string, object> actual)
    {
        var expected = new Dictionary<string, object>
        {
            { "Prop", (long)123 }
        };

        Assert.AreEqual(expected["Prop"], actual["Prop"]);
    }

    private static void AssertDecimalArray(List<object> actual)
    {
        var expected = new List<object>
        {
            (long)1,
            (decimal)2.1
        };

        Assert.AreEqual(expected[0], actual[0]);
        Assert.AreEqual(expected[1], actual[1]);
    }

    private class Model
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }
}