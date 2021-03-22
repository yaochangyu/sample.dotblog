using System;
using System.IO;
using System.Reflection;
using System.Text;
using Lexical.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.FileSystem.TestProject
{
    [TestClass]
    public class SurveyMemoryFileSystem
    {
        [TestMethod]
        public void MemoryFileSystem_FolderStruct()
        {
            var folderPath = "A:\\TestFolder\\Test";
            var content    = "This is test string";

            using var fileSystem = CreateTestMemoryFile(folderPath, content);
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

        [TestMethod]
        public void 列舉根路徑底下所有結構()
        {
            using var fileSystem = new MemoryFileSystem();
            Console.WriteLine("建立資料夾");
            fileSystem.CreateDirectory("dir1/dir2/dir3/");
            fileSystem.PrintTo(Console.Out);
            var content      = "This is test string";
            var contentBytes = Encoding.UTF8.GetBytes($"{content}");

            Console.WriteLine("dir1 底下建立檔案");
            using (var outputStream =
                fileSystem.Open("dir1/1.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                outputStream.Write(bytes, 0, bytes.Length);
            }

            Console.WriteLine("dir2 底下建立檔案");
            fileSystem.CreateFile("dir1/dir2/2.txt", contentBytes);
            var tree = fileSystem.VisitTree();

            foreach (var line in tree)
            {
                Console.WriteLine($"name:{line.Name},path:{line.Path}");
            }
        }

        [TestMethod]
        public void 列舉根路徑內的子資料夾()
        {
            using var fileSystem = new MemoryFileSystem();
            Console.WriteLine("建立資料夾");
            fileSystem.CreateDirectory("dir1/dir2/dir3/");
            fileSystem.PrintTo(Console.Out);
            var content      = "This is test string";
            var contentBytes = Encoding.UTF8.GetBytes($"{content}");

            Console.WriteLine("dir1 底下建立檔案");
            using (var outputStream =
                fileSystem.Open("dir1/1.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                outputStream.Write(bytes, 0, bytes.Length);
            }

            Console.WriteLine("dir2 底下建立檔案");
            fileSystem.CreateFile("dir1/dir2/2.txt", contentBytes);

            foreach (var entry in fileSystem.Browse(""))
            {
                var path = entry.Path;
                Console.WriteLine(path);
            }
        }

        [TestMethod]
        public void 在資料夾內建立檔案()
        {
            using var fileSystem = new MemoryFileSystem();
            Console.WriteLine("建立資料夾");
            fileSystem.CreateDirectory("dir1/dir2/dir3/");
            fileSystem.PrintTo(Console.Out);
            var content      = "This is test string";
            var contentBytes = Encoding.UTF8.GetBytes($"{content}");

            Console.WriteLine("dir1 底下建立檔案");
            using (var outputStream =
                fileSystem.Open("dir1/1.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                outputStream.Write(bytes, 0, bytes.Length);
            }

            Console.WriteLine("dir2 底下建立檔案");
            fileSystem.CreateFile("dir1/dir2/2.txt", contentBytes);

            fileSystem.PrintTo(Console.Out);
        }

        [TestMethod]
        public void 刪除資料夾()
        {
            using var fileSystem = new MemoryFileSystem();
            Console.WriteLine("建立資料夾");
            fileSystem.CreateDirectory("dir1/dir2/dir3/");
            fileSystem.PrintTo(Console.Out);

            Console.WriteLine("刪除 dir2 資料夾");
            fileSystem.Delete("dir1/dir2/", true);
            fileSystem.PrintTo(Console.Out);
        }

        [TestMethod]
        public void 建立資料夾()
        {
            using var fileSystem = new MemoryFileSystem();
            Console.WriteLine("建立資料夾");
            fileSystem.CreateDirectory("dir1/dir2/dir3/");
            fileSystem.PrintTo(Console.Out);
        }

        [TestMethod]
        public void 修改真實檔案日期()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootFolderPath    = Path.GetDirectoryName(executingAssembly.Location);
            var subFolder         = "TestFolder";
            var content           = "This is test string";

            if (Directory.Exists(subFolder) == false)
            {
                Directory.CreateDirectory(subFolder);
            }

            for (var i = 0; i < 5; i++)
            {
                var filePath = Path.Combine(rootFolderPath, subFolder, $"{i}.txt");

                var contentBytes = Encoding.UTF8.GetBytes($"{i}.{content}");
                File.WriteAllBytes(filePath, contentBytes);
            }

            //修改日期
            for (var i = 0; i < 5; i++)
            {
                var filePath = Path.Combine(rootFolderPath, subFolder, $"{i}.txt");
                File.SetCreationTime(filePath, new DateTime(2021,   1, 1));
                File.SetLastWriteTime(filePath, new DateTime(2021,  1, 1));
                File.SetLastAccessTime(filePath, new DateTime(2021, 1, 1));
            }

            //刪除檔案
            using var fileSystem = new Lexical.FileSystem.FileSystem(rootFolderPath);
            fileSystem.Delete(Path.Combine(subFolder), true);
        }

        [TestMethod]
        public void 修改檔案日期()
        {
            using var fileSystem = new MemoryFileSystem();
            Console.WriteLine("建立資料夾");
            fileSystem.CreateDirectory("dir1/dir2/dir3/");
            fileSystem.PrintTo(Console.Out);
            var content      = "This is test string";
            var contentBytes = Encoding.UTF8.GetBytes($"{content}");

            Console.WriteLine("dir2 底下建立檔案");
            fileSystem.CreateFile("dir1/dir2/2.txt", contentBytes);

            var entry = fileSystem.GetEntry("dir1/dir2/2.txt");
            Console.WriteLine("檔案修改前的日期");
            Console.WriteLine($"LastAccess:{entry.LastAccess}");
            Console.WriteLine($"LastModified:{entry.LastModified}");

            var type                     = entry.GetType();
            var now                      = new DateTimeOffset(DateTime.UtcNow.AddDays(-30));
            var lastAccessPropertyInfo   = type.GetProperty("LastAccess");
            var lastModifiedPropertyInfo = type.GetProperty("LastModified");
            lastAccessPropertyInfo.SetValue(entry, now);
            lastModifiedPropertyInfo.SetValue(entry, now);

            Console.WriteLine("檔案修改後的日期");
            Console.WriteLine($"LastAccess:{entry.LastAccess}");
            Console.WriteLine($"LastModified:{entry.LastModified}");
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