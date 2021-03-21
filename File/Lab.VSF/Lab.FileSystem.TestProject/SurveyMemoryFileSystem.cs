using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lexical.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.FileSystem.TestProject
{
    [TestClass]
    public class SurveyMemoryFileSystem
    {
        [TestMethod]
        public void MemoryFileSystem_CreateFolder()
        {
            IFileSystem filesystem = new MemoryFileSystem();

            filesystem.CreateDirectory("dir1/dir2/dir3/");
            filesystem.PrintTo(Console.Out);

            var results          = new List<string>();
            var directoryContent = filesystem.Browse("dir1/");
            foreach (var entry in directoryContent)
            {
                var path = entry.Path;
                results.Add(path);
            }
        }

        [TestMethod]
        public void MemoryFileSystem_FolderStruct()
        {
            var folderPath = "A:\\TestFolder\\Test";
            var content    = "This is test string";

            var fileSystem = CreateTestMemoryFile(folderPath, content);
            fileSystem.PrintTo(Console.Out);
            var adapter = new FileAdapter(fileSystem);
            var actual  = adapter.GetFileNames(folderPath);
            Assert.IsTrue(actual.Count > 0);
        }

        [TestMethod]
        [Ignore]
        public void VirtualFileSystem_ModifyFileDate()
        {
            IFileSystem fileSystem = new VirtualFileSystem()
                                     .Mount("tmp/", Lexical.FileSystem.FileSystem.Temp)
                                     .Mount("ram/", MemoryFileSystem.Instance);

            var directoryContent = fileSystem.Browse("tmp/");
            var type             = typeof(FileEntry);
            var offset           = new DateTimeOffset(DateTime.UtcNow.AddDays(-3));

            foreach (var entry in fileSystem.Browse("tmp/"))
            {
                var fileName                 = entry.Name;
                var filePath                 = entry.Path;
                var lastAccessPropertyInfo   = type.GetProperty("LastAccess");
                var lastModifiedPropertyInfo = type.GetProperty("LastModified");
                lastAccessPropertyInfo.SetValue(entry, offset);
                lastModifiedPropertyInfo.SetValue(entry, offset);
            }
        }

        private static MemoryFileSystem CreateTestMemoryFile(string folderPath, string content)
        {
            var memoryFileSystem = new MemoryFileSystem();

            memoryFileSystem.CreateDirectory(folderPath);
            var directory = memoryFileSystem.Browse(folderPath);
            var folder    = directory.FileSystem;

            for (var i = 0; i < 5; i++)
            {
                var filePath = $"{folderPath}/{i}.txt";

                // var filePath =  $"{folderPath}\\{i}.txt";

                // via stream
                using (var outputStream =
                    folder.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    Write(outputStream, $"{i}.{content}");
                }

                // via IFileSystem.Create
                var contentBytes = Encoding.UTF8.GetBytes($"{i}.{content}");
                folder.CreateFile(filePath, contentBytes);
            }

            var type   = typeof(FileEntry);
            var offset = new DateTimeOffset(DateTime.UtcNow.AddDays(-3));
            foreach (var entry in memoryFileSystem.Browse(folderPath))
            {
                var lastAccessPropertyInfo   = type.GetProperty("LastAccess");
                var lastModifiedPropertyInfo = type.GetProperty("LastModified");
                lastAccessPropertyInfo.SetValue(entry, offset);
                lastModifiedPropertyInfo.SetValue(entry, offset);
            }

            foreach (var entry in memoryFileSystem.Browse(folderPath))
            {
                var path = entry.Path;
            }

            return memoryFileSystem;
        }

        private static void Write(Stream stream, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}