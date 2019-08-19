using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zlib;

namespace Lab.Compress.ViaDecompressHandler.Handlers
{
    public class DecompressHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                 CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post)
            {
                var sourceContent = request.Content;
                var encodings = sourceContent.Headers.ContentEncoding;
                var isGzip = encodings.Contains("gzip");
                var isDeflate = !isGzip && encodings.Contains("deflate");

                if (isGzip || isDeflate)
                {
                    var compressStream = await sourceContent.ReadAsStreamAsync();
                    var decompressStream = new MemoryStream();
                    if (isGzip)
                    {
                        using (var gzipStream = new GZipStream(compressStream,
                                                               CompressionMode.Decompress,
                                                               CompressionLevel.BestCompression,
                                                               true))
                        {
                            await gzipStream.CopyToAsync(decompressStream);
                        }
                    }
                    else if (isDeflate)
                    {
                        using (var gzipStream = new DeflateStream(compressStream,
                                                                  CompressionMode.Decompress,
                                                                  CompressionLevel.BestCompression,
                                                                  true))
                        {
                            await gzipStream.CopyToAsync(decompressStream);
                        }
                    }

                    decompressStream.Seek(0, SeekOrigin.Begin);

                    var targetContent = new StreamContent(decompressStream);

                    foreach (var header in sourceContent.Headers)
                    {
                        targetContent.Headers.Add(header.Key, header.Value);
                    }

                    request.Content = targetContent;
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}