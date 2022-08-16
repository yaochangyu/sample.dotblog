using System.Collections.Concurrent;
using System.Reflection;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Lab.Aws.S3.MinIOS3;

public class FieldTypeAssistant
{
    private static ConcurrentDictionary<Type, Dictionary<string, string>> s_fieldTypeList = new();

    public static Dictionary<string, string> GetStaticFieldValues<T>()
    {
        var type = typeof(T);
        var fieldTypeList = s_fieldTypeList;
        if (fieldTypeList.TryGetValue(type, out var results))
        {
            return results;
        }

        var bindingFlags = BindingFlags.Public
                           | BindingFlags.Static
            ;
        results = new Dictionary<string, string>();
        var fieldInfosInfos = type.GetFields(bindingFlags);
        foreach (var fieldInfo in fieldInfosInfos)
        {
            var key = fieldInfo.Name;
            var value = fieldInfo.GetValue(null);

            results.Add(value.ToString(), key);
        }

        fieldTypeList.TryAdd(type, results);
        return results;
    }
}

public class ProfileFieldNames
{
    public const string BB1Name = "BB1";

    public const string BB2Name = "BB2";

    private static readonly Lazy<IReadOnlyDictionary<string, string>> s_valueDictionary =
        new(FieldTypeAssistant.GetStaticFieldValues<ProfileFieldNames>());

    public static IReadOnlyDictionary<string, string> GetValues()
    {
        return s_valueDictionary.Value;
    }

    public static string GetValue(string key)
    {
        s_valueDictionary.Value.TryGetValue(key, out var value);
        return value;
    }
}

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void test()
    {
        var actual = ProfileFieldNames.GetValue("BB1");
        Assert.AreEqual("BB1Name", actual);
    }

    [TestMethod]
    public async Task 新增一個儲存桶()
    {
        var s3Config = new AmazonS3Config()
        {
            RegionEndpoint = RegionEndpoint.USEast1,
            ServiceURL = "http://localhost:9000",
            ForcePathStyle = true
        };
        var s3Client = new AmazonS3Client(s3Config);
        var response = await s3Client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = "test-bucket",
        });
    }
}