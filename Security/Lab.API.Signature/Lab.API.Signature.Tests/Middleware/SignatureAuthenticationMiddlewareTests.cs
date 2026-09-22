using System.Text;
using CSharpFunctionalExtensions;
using Lab.API.Signature.Middleware;
using Lab.API.Signature.Models;
using Lab.API.Signature.Repositories;
using Lab.API.Signature.Services;
using Microsoft.AspNetCore.Http;

namespace Lab.API.Signature.Tests.Middleware;

/// <summary>
/// 驗證 High 1 修法：Middleware 必須先做「便宜檢查」（Header/QueryString/Timestamp），
/// 只有通過之後才會去讀（buffer）request body。用一個 read 就丟例外的 <see cref="ThrowingStream"/>
/// 當作 Request.Body，只要便宜檢查失敗時完全沒有丟例外，就代表 body 真的沒有被讀取。
/// </summary>
public class SignatureAuthenticationMiddlewareTests
{
    private const string ApiKey = "demo-api-key-001";

    /// <summary>Read 一律丟例外的 Stream，用來偵測「body 是否真的被讀取」。</summary>
    private sealed class ThrowingStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new InvalidOperationException("Body 不應該在便宜檢查失敗前被讀取。");

        public override long Position
        {
            get => throw new InvalidOperationException("Body 不應該在便宜檢查失敗前被讀取。");
            set => throw new InvalidOperationException("Body 不應該在便宜檢查失敗前被讀取。");
        }

        public override void Flush() => throw new InvalidOperationException("Body 不應該被讀取。");

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("Body 不應該在便宜檢查失敗前被讀取。");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Body 不應該在便宜檢查失敗前被讀取。");

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Body 不應該在便宜檢查失敗前被讀取。");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>只要被呼叫就讓測試失敗的假 Repository，用來確認便宜檢查失敗時完全不會查 DB。</summary>
    private sealed class MustNotBeCalledRepository : IApiKeyClientRepository
    {
        public Task<Maybe<ApiKeyClient>> FindByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("便宜檢查失敗時不應該查詢 Repository。");
    }

    /// <summary>只要被呼叫就讓測試失敗的假 NonceStore，用來確認驗證失敗時不會登記 Nonce。</summary>
    private sealed class MustNotBeCalledNonceStore : INonceStore
    {
        public Task<bool> TryConsumeAsync(string apiKey, string nonce, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("驗證失敗時不應該呼叫 NonceStore。");
    }

    private static DefaultHttpContext CreateContext(
        string method,
        string path,
        Stream body,
        string? apiKey = ApiKey,
        string? timestamp = null,
        string? nonce = "nonce-001",
        string? signature = "deadbeef",
        string? queryString = null,
        long? contentLength = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.Body = body;
        context.Response.Body = new MemoryStream();

        if (apiKey is not null)
        {
            context.Request.Headers["X-Api-Key"] = apiKey;
        }

        if (timestamp is not null)
        {
            context.Request.Headers["X-Timestamp"] = timestamp;
        }

        if (nonce is not null)
        {
            context.Request.Headers["X-Nonce"] = nonce;
        }

        if (signature is not null)
        {
            context.Request.Headers["X-Signature"] = signature;
        }

        if (queryString is not null)
        {
            context.Request.QueryString = new QueryString(queryString);
        }

        if (contentLength is not null)
        {
            context.Request.ContentLength = contentLength;
        }

        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task InvokeAsync_MissingHeader_DoesNotReadBody()
    {
        var context = CreateContext(
            "POST", "/api/protected/orders", new ThrowingStream(), apiKey: null, timestamp: "1700000000");

        var nextCalled = false;
        var middleware = new SignatureAuthenticationMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var handler = new SignatureValidationHandler(new MustNotBeCalledRepository());

        await middleware.InvokeAsync(context, handler, new MustNotBeCalledNonceStore());

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("\"reason\":\"HeaderMissing\"", body);
    }

    [Fact]
    public async Task InvokeAsync_QueryStringPresent_DoesNotReadBody()
    {
        var context = CreateContext(
            "GET", "/api/protected/orders/1", new ThrowingStream(),
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), queryString: "?foo=bar");

        var middleware = new SignatureAuthenticationMiddleware(_ => Task.CompletedTask);
        var handler = new SignatureValidationHandler(new MustNotBeCalledRepository());

        await middleware.InvokeAsync(context, handler, new MustNotBeCalledNonceStore());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("\"reason\":\"QueryStringNotAllowed\"", body);
    }

    [Fact]
    public async Task InvokeAsync_TimestampInvalidFormat_DoesNotReadBody()
    {
        var context = CreateContext(
            "GET", "/api/protected/orders/1", new ThrowingStream(), timestamp: "not-a-unix-timestamp");

        var middleware = new SignatureAuthenticationMiddleware(_ => Task.CompletedTask);
        var handler = new SignatureValidationHandler(new MustNotBeCalledRepository());

        await middleware.InvokeAsync(context, handler, new MustNotBeCalledNonceStore());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("\"reason\":\"TimestampInvalidFormat\"", body);
    }

    [Fact]
    public async Task InvokeAsync_TimestampOutOfRange_DoesNotReadBodyAndDoesNotThrow()
    {
        var context = CreateContext(
            "GET", "/api/protected/orders/1", new ThrowingStream(), timestamp: long.MaxValue.ToString());

        var middleware = new SignatureAuthenticationMiddleware(_ => Task.CompletedTask);
        var handler = new SignatureValidationHandler(new MustNotBeCalledRepository());

        await middleware.InvokeAsync(context, handler, new MustNotBeCalledNonceStore());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("\"reason\":\"TimestampInvalidFormat\"", body);
    }

    [Fact]
    public async Task InvokeAsync_ContentLengthExceedsLimit_ReturnsPayloadTooLargeWithoutReadingBody()
    {
        var context = CreateContext(
            "POST", "/api/protected/orders", new ThrowingStream(),
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
            contentLength: 128 * 1024); // 超過 64KB 上限

        var middleware = new SignatureAuthenticationMiddleware(_ => Task.CompletedTask);
        var handler = new SignatureValidationHandler(new MustNotBeCalledRepository());

        await middleware.InvokeAsync(context, handler, new MustNotBeCalledNonceStore());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("\"reason\":\"PayloadTooLarge\"", body);
    }

    [Fact]
    public async Task InvokeAsync_ActualBodyExceedsLimitWithoutContentLength_ReturnsPayloadTooLarge()
    {
        // 沒有 Content-Length（例如 chunked transfer），但實際 body 超過上限，
        // 第二道防線（CopyWithLimitAsync）應該擋下來，而不是把整包超大 body 讀進記憶體。
        var oversizedBody = new MemoryStream(Encoding.UTF8.GetBytes(new string('a', 128 * 1024)));
        var context = CreateContext(
            "POST", "/api/protected/orders", oversizedBody,
            timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());

        var middleware = new SignatureAuthenticationMiddleware(_ => Task.CompletedTask);
        var handler = new SignatureValidationHandler(new MustNotBeCalledRepository());

        await middleware.InvokeAsync(context, handler, new MustNotBeCalledNonceStore());

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(context);
        Assert.Contains("\"reason\":\"PayloadTooLarge\"", body);
    }
}
