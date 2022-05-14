using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.JsonDiffPatch;
using System.Text.Json.JsonDiffPatch.MsTest;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.JsonCompare.UnitTest;

[TestClass]
public class SystemTextJsonDiffPathTests
{
    [TestMethod]
    public void 比對兩個一樣的Json字串_via_JsonDiffPatcher()
    {
        var source = new JsonObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2) }
        };

        var dest = new JsonObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2) }
        };

        var left = source.ToJsonString();
        var right = dest.ToJsonString();
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
        var source = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2) }
        };

        var dest = new JsonObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };

        var diff = source.Diff(dest);
        if (diff != null)
        {
            Console.WriteLine(JsonSerializer.Serialize(diff));
        }

        Assert.IsNotNull(diff);
    }

    [TestMethod]
    public void 比對兩個不一樣的JsonNode()
    {
        var source = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2) }
        };

        var dest = new JsonObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };

        var left = JsonNode.Parse(source.ToJsonString());
        var right = JsonNode.Parse(dest.ToJsonString());
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
        var source = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2) }
        };

        var dest = new JsonObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };
        Assert.That.JsonAreEqual(source, dest, true);
    }

    [TestMethod]
    public void 比對兩個不一樣的JsonDocument()
    {
        var source = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2) }
        };

        var dest = new JsonObject
        {
            { "Integer", 12345 },
            { "String", JsonValue.Create("A string") },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };

        var left = JsonDocument.Parse(source.ToJsonString());
        var right = JsonDocument.Parse(dest.ToJsonString());

        var isEquals = left.DeepEquals(right);
        Assert.IsFalse(isEquals);
    }

    [TestMethod]
    public void 更新節點()
    {
        var source = new JsonObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2, new JsonArray { "a", "b" }) }
        };

        var dest = new JsonObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JsonArray(1, 2) }
        };
        
        var left = JsonNode.Parse(dest.ToJsonString());
        var right = JsonNode.Parse(source.ToJsonString());
        
        //左邊不等於來源，跟我認知的不一樣
        var diff = left.Diff(right);
        JsonDiffPatcher.Patch(ref left, diff);

        Assert.That.JsonAreEqual(right, right, true);
    }
}