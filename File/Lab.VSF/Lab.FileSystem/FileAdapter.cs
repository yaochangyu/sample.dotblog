using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lexical.FileSystem;

namespace Lab.FileSystem
{
    public class FileAdapter : IFileAdapter
    {
        internal DateTime Now
        {
            get
            {
                if (this._now.HasValue == false)
                {
                    return DateTime.UtcNow;
                }

                return this._now.Value;
            }
            set => this._now = value;
        }

        private readonly IFileSystem _fileSystem;
        private          DateTime?   _now;

        public FileAdapter(IFileSystem fileSystem)
        {
            this._fileSystem = fileSystem;
        }

        public Dictionary<string, string> GetContents(string folderPath)
        {
            var fileSystem = this._fileSystem;

            var results = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            if (fileSystem.Exists(folderPath) == false)
            {
                return results;
            }

            foreach (var entry in fileSystem.Browse(folderPath))
            {
                var path = entry.Path;

                using (var inputStream = entry.FileSystem.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var content = Read(inputStream);

                    results.Add(path, content);
                }
            }

            return results;
        }

        public ICollection<string> GetFileNames(string folderPath)
        {
            var fileSystem = this._fileSystem;

            var results = new List<string>();

            // if (fileSystem.Browse(folderPath).Exists ==false)
            // {
            //     return results;
            // }
            if (fileSystem.Exists(folderPath) == false)
            {
                return results;
            }

            foreach (var entry in fileSystem.Browse(folderPath))
            {
                results.Add(entry.Path);
            }

            return results;
        }

        public void DeleteAgo(string folderPath, int day)
        {
            var fileSystem = this._fileSystem;
            var now = this.Now;

            if (fileSystem.Exists(folderPath) == false)
            {
                return;
            }

            foreach (var entry in fileSystem.Browse(folderPath))
            {
                var diff = now - entry.LastModified.Date;
                if (diff.Days > 2)
                {
                    fileSystem.Delete(entry.Path);
                }
            }
        }

        private static string Read(Stream stream)
        {
            var buffer = new byte[1024];
            int length;
            var builder = new StringBuilder();
            while ((length = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var content = Encoding.UTF8.GetString(buffer, 0, length);
                Console.WriteLine(content);
                builder.Append(content);
            }

            return builder.ToString();
        }
    }
}