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
        JObject o1 = new JObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JArray(1, 2) }
        };

        JObject o2 = new JObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JArray(1, 2) }
        };
        var isEquals = JToken.DeepEquals(o1, o2);
        Assert.IsTrue(isEquals);
    }

    [TestMethod]
    public void 比對兩個不一樣的JObject()
    {
        var o1 = new JObject
        {
            { "Integer", 12345 },
            { "String", "A string" },
            { "Items", new JArray(1, 2) }
        };

        var o2 = new JObject
        {
            { "integer", 12345 },
            { "String", "A string" },
            { "Items", new JArray(1, 2, new JArray { "a", "b" }) }
        };
        var diffPath = new JsonDiffPatch();
        var diff = diffPath.Diff(o1, o2);

        if (diff != null)
        {
            Console.WriteLine(diff.ToString());
        }
        
        Assert.IsNotNull(diff);
    }
}