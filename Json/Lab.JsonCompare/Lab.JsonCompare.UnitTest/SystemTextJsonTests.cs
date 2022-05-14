using System;
using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.MsTest;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.JsonCompare.UnitTest;

[TestClass]
public class SystemTextJsonTests
{
    [TestMethod]
    public void 比對兩個一樣的Json字串_via_JsonDiffPatcher()
    {
        var o1 = new JsonObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2) }
        };

        var o2 = new JsonObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2) }
        };

        var left = o1.ToJsonString();
        var right = o2.ToJsonString();
        var diff = JsonDiffPatcher.Diff(left, right);
        if (diff != null)
        {
            Console.WriteLine(JsonSerializer.Serialize(diff));
        }

        Assert.IsNull(diff);
    }

    [TestMethod]
    public void 比對兩個不一樣的JsonObject()
    {
        var o1 = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2) }
        };

        var o2 = new JsonObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };

        var diff = o1.Diff(o2);
        if (diff != null)
        {
            Console.WriteLine(JsonSerializer.Serialize(diff));
        }

        Assert.IsNotNull(diff);
    }

    [TestMethod]
    public void 比對兩個不一樣的JsonNode()
    {
        var o1 = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2) }
        };

        var o2 = new JsonObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };

        var left = JsonNode.Parse(o1.ToJsonString());
        var right = JsonNode.Parse(o2.ToJsonString());
        var diff = left.Diff(right);
        if (diff != null)
        {
            Console.WriteLine(JsonSerializer.Serialize(diff));
        }

        Assert.IsNotNull(diff);
    }

    [TestMethod]
    public void 比對兩個不一樣的JsonObject_via_JsonAssert()
    {
        var o1 = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2) }
        };

        var o2 = new JsonObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };
        Assert.That.JsonAreEqual(o1, o2, true);
    }

    [TestMethod]
    public void 比對兩個不一樣的JsonDocument()
    {
        var o1 = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2) }
        };

        var o2 = new JsonObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };

        var left = JsonDocument.Parse(o1.ToJsonString());
        var right = JsonDocument.Parse(o2.ToJsonString());

        var isEquals = left.DeepEquals(right);
        Assert.IsFalse(isEquals);
    }
}