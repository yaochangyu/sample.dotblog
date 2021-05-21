using System;

namespace NET5.TestProject.File
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