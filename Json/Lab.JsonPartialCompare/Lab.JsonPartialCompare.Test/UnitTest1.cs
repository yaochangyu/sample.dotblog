using System.Text.Json.Nodes;
using Xunit;

namespace Lab.JsonPartialCompare.Test;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var leftJson =
            """
            {
              "Id": 1,
              "Birthday": "2000-01-01T00:00:00+00:00",
              "FullName": {
                "LastName": "Doe"
              }
            }
            """;

        var rightJson =
            """
            {
              "Id": 1,
              "Age": 18,
              "Birthday": "2000-01-01T00:00:00+00:00",
              "FullName": {
                "FirstName": "John",
                "LastName": "Doe"
              },
              "Addresses": [
                {
                  "Address1": "Address1",
                  "Address2": "Address2"
                }
              ]
            }
            """;
        var leftNode = JsonNode.Parse(leftJson);
        var rightNode = JsonNode.Parse(rightJson);

        var result = JsonCompare.Diff(leftNode, rightNode);
        Assert.True(result.IsValidated, result.ErrorReason);
    }
}

class JsonCompare
{
    public static JsonDiffResult Diff(JsonNode left, JsonNode right)
    {
        if (left is JsonObject leftObject
            && right is JsonObject rightObject)
        {
            foreach (var property in leftObject)
            {
                if (!rightObject.TryGetPropertyValue(property.Key, out JsonNode rightProperty))
                {
                    return new JsonDiffResult(false, $"Property '{property.Key}' is missing in the right JSON.");
                }

                var result = Diff(property.Value, rightProperty);
                if (!result.IsValidated)
                {
                    return new JsonDiffResult(false, $"Property '{property.Key}' mismatch: {result.ErrorReason}");
                }
            }

            return new JsonDiffResult(true, null);
        }

        if (left is JsonArray leftArray
            && right is JsonArray rightArray)
        {
            if (leftArray.Count != rightArray.Count)
            {
                return new JsonDiffResult(false, "Array length mismatch.");
            }

            for (var i = 0; i < leftArray.Count; i++)
            {
                var result = Diff(leftArray[i], rightArray[i]);
                if (!result.IsValidated)
                {
                    return new JsonDiffResult(false,
                        $"Array element at index {i} mismatch: {result.ErrorReason}");
                }
            }

            return new JsonDiffResult(true, null);
        }

        if (left.ToJsonString() != right.ToJsonString())
        {
            return new JsonDiffResult(false,
                $"Value mismatch: '{left.ToJsonString()}' vs '{right.ToJsonString()}'");
        }

        return new JsonDiffResult(true, null);
    }
}

class JsonDiffResult
{
    public bool IsValidated { get; }

    public string ErrorReason { get; }

    public JsonDiffResult(bool isValidated, string errorReason)
    {
        this.IsValidated = isValidated;
        this.ErrorReason = errorReason;
    }

    static JsonDiffResult JsonCompare(JsonNode left, JsonNode right)
    {
        if (left is JsonObject leftObject
            && right is JsonObject rightObject)
        {
            foreach (var property in leftObject)
            {
                if (!rightObject.TryGetPropertyValue(property.Key, out JsonNode rightProperty))
                {
                    return new JsonDiffResult(false, $"Property '{property.Key}' is missing in the right JSON.");
                }

                var result = JsonCompare(property.Value, rightProperty);
                if (!result.IsValidated)
                {
                    return new JsonDiffResult(false, $"Property '{property.Key}' mismatch: {result.ErrorReason}");
                }
            }

            return new JsonDiffResult(true, null);
        }

        if (left is JsonArray leftArray && right is JsonArray rightArray)
        {
            if (leftArray.Count != rightArray.Count)
            {
                return new JsonDiffResult(false, "Array length mismatch.");
            }

            for (var i = 0; i < leftArray.Count; i++)
            {
                var result = JsonCompare(leftArray[i], rightArray[i]);
                if (!result.IsValidated)
                {
                    return new JsonDiffResult(false,
                        $"Array element at index {i} mismatch: {result.ErrorReason}");
                }
            }

            return new JsonDiffResult(true, null);
        }

        if (left.ToJsonString() != right.ToJsonString())
        {
            return new JsonDiffResult(false,
                $"Value mismatch: '{left.ToJsonString()}' vs '{right.ToJsonString()}'");
        }

        return new JsonDiffResult(true, null);
    }
}