using System;

namespace Server
{
    public class ZipFileProvider : IFileProvider
    {
        public string Print()
        {
            var msg = "ZipFileProvider";
            Console.WriteLine(msg);
            return msg;
        }
    }
}