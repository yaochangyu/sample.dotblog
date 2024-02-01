using System.Collections.Concurrent;
using System.Reflection;
using Amazon.Runtime;
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
        var s3Client = CreateS3Client();
        var response = await s3Client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = "test-bucket",
        });
    }

    [TestMethod]
    public async Task 分批上傳檔案()
    {
        await WriteObjectDataAsync("上傳.csv");
    }
    [TestMethod]
    public async Task 分批下載檔案()
    {
        await WriteObjectDataAsync("上傳.csv");
    }

    static async Task WriteObjectDataAsync(string filePath)
    {
        var s3Client = CreateS3Client();
        var writerStream = new MemoryStream();
        var writer = new StreamWriter(writerStream)
        {
            AutoFlush = true
        };
        try
        {
            await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(fileStream);
            var lineCount = 0;
            var lineLimit = 10;
            var putRequest = new PutObjectRequest()
            {
                BucketName = "test-bucket",
                Key = $"test.csv",
                UseChunkEncoding = true,
                AutoCloseStream = false,
                AutoResetStreamPosition = true,
            };
            while (await reader.ReadLineAsync() is { } line)
            {
                // todo:可以處理一整批後再寫入到 s3
                await writer.WriteLineAsync(line);
                putRequest.InputStream = writerStream;
                var response = await s3Client.PutObjectAsync(putRequest);
                lineCount++;
            }
        }
        catch (AmazonS3Exception e)
        {
            Console.WriteLine("Error encountered on server. Message:'{0}' when writing object", e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing object", e.Message);
        }
    }
    
    static AmazonS3Client CreateS3Client()
    {
        var credentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE",
            "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");

        var s3Config = new AmazonS3Config
        {
            ServiceURL = "http://localhost:9000",
            ForcePathStyle = true
        };
        return new AmazonS3Client(credentials, s3Config);
    }
}