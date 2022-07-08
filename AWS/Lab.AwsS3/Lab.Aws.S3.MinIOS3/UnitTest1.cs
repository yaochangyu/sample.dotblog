using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace Lab.Aws.S3.MinIOS3;

[TestClass]
public class UnitTest1
{
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