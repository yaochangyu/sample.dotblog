using System;
using JsonDiffPatchDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Lab.JsonCompare.UnitTest;

[TestClass]
public class NewtonsoftJsonDiffPathTests
{
    [TestMethod]
    public void 比對兩個一樣的JObject()
    {
        var source = new JObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JArray(1, 2) }
        };

        var dest = new JObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JArray(1, 2) }
        };
        var isEquals = JToken.DeepEquals(source, dest);
        Assert.IsTrue(isEquals);
    }

    [TestMethod]
    public void 比對兩個不一樣的JObject()
    {
        var source = new JObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JArray(1, 2) }
        };

        var dest = new JObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JArray(1, 2, new JArray { "a", "b" }) }
        };
        var diffPath = new JsonDiffPatch();
        var diff = diffPath.Diff(source, dest);

        if (diff != null)
        {
            Console.WriteLine(diff.ToString());
        }
        
        Assert.IsNotNull(diff);
    }
}