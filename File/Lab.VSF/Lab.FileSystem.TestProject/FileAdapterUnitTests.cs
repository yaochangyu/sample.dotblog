using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Lexical.FileSystem;
using Lexical.FileSystem.Decoration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.FileSystem.TestProject
{
    [TestClass]
    public class FileAdapterUnitTests
    {
        [TestMethod]
        public void FileSystem_DeleteAgo()
        {
            //arrange
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootFolderPath    = Path.GetDirectoryName(executingAssembly.Location);
            var targetFolderName  = "TestFolder";
            var content           = "This is test string";

            using (var fileSystem = CreateTestFile(rootFolderPath, targetFolderName, content))
            {
                var adapter = new FileAdapter(fileSystem);

                //act
                adapter.DeleteAgo(targetFolderName, 2);

                //assert
                var directoryContent = fileSystem.Browse("targetFolder");
                Assert.AreEqual(true, directoryContent.Any() == false);

                //restore
                fileSystem.Delete(targetFolderName, true);
            }
        }

        [TestMethod]
        public void FileSystem_GetContents()
        {
            //arrange
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootFolderPath    = Path.GetDirectoryName(executingAssembly.Location);

            var targetFolderName = "TestFolder";
            var content          = "This is test string";

            using (var fileSystem = CreateTestFile(rootFolderPath, targetFolderName, content))
            {
                var adapter = new FileAdapter(fileSystem);

                //act
                var actual = adapter.GetContents(targetFolderName);

                //assert
                Assert.IsTrue(actual.Count > 0);

                //restore
                fileSystem.Delete(targetFolderName, true);
            }
        }

        [TestMethod]
        public void FileSystem_GetFileNames()
        {
            //arrange
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootFolderPath    = Path.GetDirectoryName(executingAssembly.Location);
            var targetFolderName  = "TestFolder";
            var content           = "This is test string";

            using (var fileSystem = CreateTestFile(rootFolderPath, targetFolderName, content))
            {
                var adapter = new FileAdapter(fileSystem);

                //act
                var actual = adapter.GetFileNames(targetFolderName);

                //assert
                Assert.IsTrue(actual.Count > 0);

                //restore
                fileSystem.Delete(targetFolderName, true);
            }
        }

        [TestMethod]
        public void MemoryFileSystem_DeleteAgo()
        {
            //arrange
            var rootFolderPath = "A:\\TestFolder\\Test";
            var content        = "This is test string";
            using (var fileSystem = CreateTestMemoryFile(rootFolderPath, content))
            {
                var adapter = new FileAdapter(fileSystem);

                //act
                adapter.DeleteAgo(rootFolderPath, 2);

                //assert
                var directoryContent = fileSystem.Browse(rootFolderPath);
                Assert.AreEqual(true, directoryContent.Any() == false);
            }
        }

        [TestMethod]
        public void MemoryFileSystem_GetContents()
        {
            //arrange
            var rootFolderPath = "A:\\TestFolder\\Test";
            var content        = "This is test string";

            using (var fileSystem = CreateTestMemoryFile(rootFolderPath, content))
            {
                var adapter = new FileAdapter(fileSystem);

                //act
                var actual = adapter.GetContents(rootFolderPath);

                //assert
                Assert.IsTrue(actual.Count > 0);
            }
        }
        [TestMethod]
        public void MemoryFileSystem_GetFileNames()
        {
            //arrange
            var rootFolderPath = "A:\\TestFolder\\Test";
            var content        = "This is test string";

            using (var fileSystem = CreateTestMemoryFile(rootFolderPath, content))
            {
                var adapter = new FileAdapter(fileSystem);

                //act
                var actual = adapter.GetFileNames(rootFolderPath);

                //assert
                Assert.IsTrue(actual.Count > 0);
            }
        }

        private static Lexical.FileSystem.FileSystem CreateTestFile(string rootFolder, string subFolder, string content)
        {
            var fileSystem = new Lexical.FileSystem.FileSystem(rootFolder);

            if (fileSystem.Exists(subFolder) == false)
            {
                fileSystem.CreateDirectory(subFolder);
            }

            for (var i = 0; i < 5; i++)
            {
                var filePath     = Path.Combine(rootFolder, subFolder, $"{i}.txt");
                var contentBytes = Encoding.UTF8.GetBytes($"{i}.{content}");
                fileSystem.CreateFile(filePath, contentBytes);
            }

            var now = DateTime.UtcNow.AddDays(-30);
            for (var i = 0; i < 5; i++)
            {
                var filePath = Path.Combine(rootFolder, subFolder, $"{i}.txt");
                File.SetLastWriteTime(filePath, now);
                File.SetLastAccessTime(filePath, now);
                File.SetCreationTime(filePath, now);
            }

            return fileSystem;
        }

        private static void CreateTestFile1(string folderPath, string content)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);

            using (var folder = new Lexical.FileSystem.FileSystem(rootPath))
            {
                if (folder.Exists(folderPath) == false)
                {
                    folder.CreateDirectory(folderPath);
                }
            }

            using (var folder = new Lexical.FileSystem.FileSystem($"{rootPath}\\{folderPath}"))
            {
                for (var i = 0; i < 5; i++)
                {
                    var filePath     = $"{i}.txt";
                    var contentBytes = Encoding.UTF8.GetBytes($"{i}.{content}");

                    folder.CreateFile(filePath, contentBytes);
                }

                folder.PrintTo(Console.Out);
            }
        }

        private static MemoryFileSystem CreateTestMemoryFile(string folderPath, string content)
        {
            var fileSystem = new MemoryFileSystem();

            fileSystem.CreateDirectory(folderPath);

            for (var i = 0; i < 5; i++)
            {
                var filePath = $"{folderPath}/{i}.txt";

                // via stream
                using (var outputStream =
                    fileSystem.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    Write(outputStream, $"{i}.{content}");
                }

                // via IFileSystem.Create
                // var contentBytes = Encoding.UTF8.GetBytes($"{i}.{content}");
                // fileSystem.CreateFile(filePath, contentBytes);
            }

            var type   = typeof(FileEntry);
            var offset = new DateTimeOffset(DateTime.UtcNow.AddDays(-3));
            foreach (var entry in fileSystem.Browse(folderPath))
            {
                var lastAccessPropertyInfo   = type.GetProperty("LastAccess");
                var lastModifiedPropertyInfo = type.GetProperty("LastModified");
                lastAccessPropertyInfo.SetValue(entry, offset);
                lastModifiedPropertyInfo.SetValue(entry, offset);
            }

            return fileSystem;
        }

        private static VirtualFileSystem CreateTestVirtualFile(string folderPath, string content)
        {
            var result = new VirtualFileSystem();

            result.CreateDirectory(folderPath);
            var directory = result.Browse(folderPath);
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
                folder.CreateFile(filePath, contentBytes, new FileProviderSystem.Options());
            }

            var type   = typeof(FileEntry);
            var offset = new DateTimeOffset(DateTime.UtcNow.AddDays(-3));
            foreach (var entry in result.Browse(folderPath))
            {
                var path = entry.Path;

                // var type                     = entry.GetType();

                // entry.LastAccess.AddDays(-2);
                // entry.LastModified.AddDays(-2);
                var lastAccessPropertyInfo   = type.GetProperty("LastAccess");
                var lastModifiedPropertyInfo = type.GetProperty("LastModified");
                lastAccessPropertyInfo.SetValue(entry, offset);
                lastModifiedPropertyInfo.SetValue(entry, offset);
            }

            foreach (var entry in result.Browse(folderPath))
            {
                var path = entry.Path;
            }

            return result;
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

        private static string Read1(Stream stream)
        {
            var buffer  = new byte[1024];
            var builder = new StringBuilder();
            while (stream.Read(buffer, 0, buffer.Length) > 0)
            {
                var content = Encoding.UTF8.GetString(buffer, 0, stream.Read(buffer, 0, buffer.Length));
                builder.Append(content);
            }

            return buffer.ToString();
        }

        private static void Write(Stream stream, string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}