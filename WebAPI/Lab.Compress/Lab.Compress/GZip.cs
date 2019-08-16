using System.IO;
using Ionic.Zlib;

namespace Lab.Compress
{
    public class GZip
    {
        public static byte[] Compress(byte[] sourceBytes)
        {
            if (sourceBytes == null)
            {
                return null;
            }

            using (var outputStream = new MemoryStream())
            {
                using (var zipStream = new GZipStream(outputStream,
                                                      CompressionMode.Compress,
                                                      CompressionLevel.BestSpeed,
                                                      true))
                {
                    zipStream.Write(sourceBytes, 0, sourceBytes.Length);
                }

                return outputStream.ToArray();
            }
        }

        public static byte[] Decompress(byte[] sourceBytes)
        {
            using (var compressedStream = new MemoryStream(sourceBytes))
            using (var zipStream = new GZipStream(compressedStream,
                                                  CompressionMode.Decompress,
                                                  CompressionLevel.BestSpeed,
                                                  true))

            using (var outputStream = new MemoryStream())
            {
                zipStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }
    }
}