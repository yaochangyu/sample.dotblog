using System;

namespace Server
{
    public class FileProvider : IFileProvider
    {
        public string Print()
        {
            var msg = "FileProvider";
            Console.WriteLine(msg);
            return msg;
        }
    }
}