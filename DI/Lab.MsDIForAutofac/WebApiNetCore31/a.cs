using System;

namespace WebApiNetCore31
{
    public interface IFileProvider
    {
        string Print();
    }

    public class FileProvider : IFileProvider
    {
        public string Print()
        {
            var msg = "FileProvider";
            Console.WriteLine(msg);
            return msg;
        }
    }

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