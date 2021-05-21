using System;

namespace NET5.TestProject.File
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