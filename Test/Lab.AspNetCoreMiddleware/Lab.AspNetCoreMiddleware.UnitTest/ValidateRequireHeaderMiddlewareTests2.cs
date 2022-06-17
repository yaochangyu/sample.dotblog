using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.JsonDiffPatch.MsTest;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.AspNetCoreMiddleware.UnitTest;

[TestClass]
public class ValidateRequireHeaderMiddlewareTests2
{
    [TestMethod]
    public async Task HeaderCode型別錯誤會驗證失敗()
    {
        var expected = @"
{
  ""code"": ""INVALID_REQUEST"",
  ""messages"": [
    {
      ""code"": ""INVALID_TYPE"",
      ""propertyName"": ""X-Code"",
      ""messages"": ""'abc' not numbers"",
      ""value"": ""abc""
    }
  ]
}
";
        var jsonSerializerOptions = CreateJsonSerializerOptions();
        var httpContext = new DefaultHttpContext()
        {
            Response = { Body = new MemoryStream()}
        };
        httpContext.Request.Headers[HeaderNames.UserId] = "yao";
        httpContext.Request.Headers[HeaderNames.Code] = "abc";
        var target = new ValidateRequiredHeaderMiddleware((_) => Task.CompletedTask);
        await target.InvokeAsync(httpContext, jsonSerializerOptions);
        var response = httpContext.Response;
        var stream = response.Body;
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        var actual = await new StreamReader(stream).ReadToEndAsync();
        Assert.That.JsonAreEqual(expected, actual, true);
    }

    [TestMethod]
    public async Task 所有Header為空會驗證失敗()
    {
        var expected = @"
{
  ""code"": ""INVALID_REQUEST"",
  ""messages"": [
    {
      ""code"": ""INVALID_FORMAT"",
      ""propertyName"": ""X-User-Id"",
      ""messages"": ""The 'X-User-Id' header is required.""
    },
    {
      ""code"": ""INVALID_FORMAT"",
      ""propertyName"": ""X-Code"",
      ""messages"": ""The 'X-Code' header is required.""
    }
  ]
}
";
        var jsonSerializerOptions = CreateJsonSerializerOptions();
        var httpContext = new DefaultHttpContext
        {
            Response = { Body = new MemoryStream() }
        };

        var target = new ValidateRequiredHeaderMiddleware(_ => Task.CompletedTask);
        await target.InvokeAsync(httpContext, jsonSerializerOptions);
        var response = httpContext.Response;
        var stream = response.Body;
        if (stream.CanSeek)
        {
            stream.Seek(0, SeekOrigin.Begin);
        }

        var actual = await new StreamReader(stream).ReadToEndAsync();
        Assert.That.JsonAreEqual(expected, actual, true);
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,

            // Encoder = JavaScriptEncoder.Create(UnicodeRanges.All, UnicodeRanges.All),
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
    }
}