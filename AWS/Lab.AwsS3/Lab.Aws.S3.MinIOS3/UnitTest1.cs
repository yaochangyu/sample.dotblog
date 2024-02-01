using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

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
        await ReadObjectDataAsync2("上傳.csv", "下載.csv");
    }

    static async Task ReadObjectDataAsync2(string sourceFile, string outFile)
    {
        var bucketName = "test-bucket";
        var key = sourceFile;

        try
        {
            var s3Client = CreateS3Client();

            long startPosition = 0;
            long chunkSize = 4096; // 每次讀取的大小為 4096 bytes

            // 確定檔案的大小
            long fileSize = await GetFileSizeAsync(s3Client, bucketName, key);

            using (var outputStream = new StreamWriter(outFile))
            {
                while (startPosition < fileSize)
                {
                    long endPosition = Math.Min(startPosition + chunkSize, fileSize) - 1;

                    // 設定 ByteRange
                    var getObjectRequest = new GetObjectRequest
                    {
                        BucketName = bucketName,
                        Key = key,
                        ByteRange = new ByteRange(startPosition, endPosition)
                    };

                    using (var response = await s3Client.GetObjectAsync(getObjectRequest))
                    using (var reader = new StreamReader(response.ResponseStream))
                    {
                        // 處理部分檔案內容
                        var buffer = new char[chunkSize];
                        int bytesRead;
                        string line = string.Empty;
                        while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            line += new string(buffer, 0, bytesRead);
                            int newlineIndex = line.IndexOf('\n');
                            if (newlineIndex >= 0)
                            {
                                await outputStream.WriteAsync(line.Substring(0, newlineIndex + 1));
                                line = line.Substring(newlineIndex + 1);
                            }
                        }

                        // 處理剩餘的部分，如果有
                        if (!string.IsNullOrEmpty(line))
                        {
                            await outputStream.WriteLineAsync(line); // 添加換行符號
                        }
                    }

                    startPosition += chunkSize;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private static async Task<long> GetFileSizeAsync(IAmazonS3 s3Client, string bucketName, string objectKey)
    {
        var metadataRequest = new GetObjectMetadataRequest
        {
            BucketName = bucketName,
            Key = objectKey
        };

        var response = await s3Client.GetObjectMetadataAsync(metadataRequest);
        return response.ContentLength;
    }

    static async Task ReadObjectDataAsync(string filePath)
    {
        var s3Client = CreateS3Client();
        var bucketName = "test-bucket";

        var fileTransferUtilityRequest = new TransferUtilityUploadRequest()
        {
        };
        var transferUtilityDownloadRequest = new TransferUtilityDownloadRequest
        {
            BucketName = null,
            Key = null,
            VersionId = null,
            ModifiedSinceDateUtc = default,
            UnmodifiedSinceDateUtc = default,
            ChecksumMode = null,
            ServerSideEncryptionCustomerProvidedKey = null,
            ServerSideEncryptionCustomerProvidedKeyMD5 = null,
            ServerSideEncryptionCustomerMethod = null,
            FilePath = null
        };
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = filePath,

            // PartNumber = 1,
            ByteRange = new ByteRange(0, 14096),
        };
        var retryCount = 0;
        while (true)
        {
            using var response = await s3Client.GetObjectAsync(getObjectRequest);
            using var reader = new StreamReader(response.ResponseStream);
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line))
                {
                    retryCount++;
                    break;
                }

                // Process the line
                Console.WriteLine(line);
                retryCount = 0;
            }

            if (retryCount >= 2)
            {
                break;
            }
        }
    }

    static async Task WriteObjectDataAsync(string filePath)
    {
        var s3Client = CreateS3Client();
        var bucketName = "test-bucket";
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
                BucketName = bucketName,
                Key = filePath,
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