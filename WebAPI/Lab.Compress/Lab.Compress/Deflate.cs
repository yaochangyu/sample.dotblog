using System.IO;
using Ionic.Zlib;

namespace Lab.Compress
{
    public class Deflate
    {
        public static byte[] Compress(byte[] sourceBytes)
        {
            if (sourceBytes == null)
            {
                return null;
            }

            using (var output = new MemoryStream())
            {
                using (var compressor = new DeflateStream(output,
                                                          CompressionMode.Compress,
                                                          CompressionLevel.BestSpeed))
                {
                    compressor.Write(sourceBytes, 0, sourceBytes.Length);
                }

                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] sourceBytes)
        {
            if (sourceBytes == null)
            {
                return null;
            }

            using (var output = new MemoryStream())
            {
                using (var compressor = new DeflateStream(output,
                                                          CompressionMode.Decompress,
                                                          CompressionLevel.BestSpeed))
                {
                    compressor.Write(sourceBytes, 0, sourceBytes.Length);
                }

                return output.ToArray();
            }
        }
    }
}