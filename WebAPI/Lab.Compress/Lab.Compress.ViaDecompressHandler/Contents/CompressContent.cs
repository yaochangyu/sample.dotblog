using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ionic.Zlib;

namespace Lab.Compress.ViaDecompressHandler
{
    public class CompressContent : HttpContent
    {
        private readonly CompressMethod _compressionMethod;
        private readonly HttpContent       _originalContent;

        public CompressContent(HttpContent content, CompressMethod compressionMethod)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            
            this._originalContent   = content;
            this._compressionMethod = compressionMethod;

            foreach (var header in this._originalContent.Headers)
            {
                this.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            this.Headers.ContentEncoding.Add(this._compressionMethod.ToString().ToLowerInvariant());
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            if (this._compressionMethod == CompressMethod.GZip)
            {
                using (var gzipStream = new GZipStream(stream, 
                                                       CompressionMode.Compress))
                {
                    await this._originalContent.CopyToAsync(gzipStream);
                }
            }
            else if (this._compressionMethod == CompressMethod.Deflate)
            {
                using (var deflateStream = new DeflateStream(stream, 
                                                             CompressionMode.Compress))
                {
                    await this._originalContent.CopyToAsync(deflateStream);
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}