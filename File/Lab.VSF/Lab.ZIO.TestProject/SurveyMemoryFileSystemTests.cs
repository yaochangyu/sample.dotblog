using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zio;
using Zio.FileSystems;

namespace Lab.ZIO.TestProject
{
    [TestClass]
    public class SurveyMemoryFileSystemTests
    {
        [TestMethod]
        public void UPathCombine()
        {
            var rootPath = "/mnt/c/Temp/Test";
            var uPath1   = UPath.Combine(rootPath, "../1");
            var uPath2   = UPath.Combine(rootPath, "./2/");
            var uPath3   = UPath.Combine(rootPath, "..");
            var uPath4   = UPath.Combine(rootPath, @"..\..\3\");
            var uPath5   = (UPath) "/this/is/a/path/to/a/directory";
            var uPath6   = (UPath) @"/this\is/wow/../an/absolute/./pat/h/";
 
            Console.WriteLine(uPath1);
            Console.WriteLine(uPath2);
            Console.WriteLine(uPath3);
            Console.WriteLine(uPath4);
            Console.WriteLine(uPath5);
            Console.WriteLine(uPath6);
        }
        [TestMethod]
        public void UPathTo()
        {
            var rootPath = "/mnt/c/Temp/Test";
            var path1    = (UPath) "/this/is/a/path/to/a/directory";
            var path2    = (UPath) @"/this\is/wow/../an/absolute/./pat/h/";
            var path3    = (UPath) @"this\is/wow/../an/absolute/./pat/h/";

            Console.WriteLine(path1);
            Console.WriteLine(path2);
            Console.WriteLine(path3);
        }
 
        [TestMethod]
        public void PathCombine()
        {
            var rootPath = @"E:\src\sample.dotblog\File";
            var path1    = Path.Combine(rootPath, "../1");
            var path2    = Path.Combine(rootPath, "./2/");
            var path3    = Path.Combine(rootPath, "..");
            var path4    = Path.Combine(rootPath, @"..\..\3\");
        
            Console.WriteLine(new DirectoryInfo(path1).FullName);
            Console.WriteLine(new DirectoryInfo(path2).FullName);
            Console.WriteLine(new DirectoryInfo(path3).FullName);
            Console.WriteLine(new DirectoryInfo(path4).FullName);
        }

        [TestMethod]
        public void 列舉根路徑內的子資料夾()
        {
            var       rootUPath  = CreateRootPath();
            using var fileSystem = new MemoryFileSystem();
            var       subName    = "../../path";

            var subPath      = $"{rootUPath}/{subName}";
            var subPath1     = $"{subPath}/1";
            var subFile1     = $"{subPath}/1/1.txt";
            var subPath1_1   = $"{subPath}/1/1_1";
            var subFile1_1   = $"{subPath}/1/1_1/1_1.txt";
            var subPath1_1_1 = $"{subPath}/1/1_1/1_1_1";
            var subPath2     = $"{subPath}/2";
            
            var uPath        = UPath.Combine(rootUPath, "..");
            
            var content      = "This is test string";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            if (fileSystem.DirectoryExists(subPath1_1_1) == false)
            {
                fileSystem.CreateDirectory(subPath1_1_1);
            }

            if (fileSystem.DirectoryExists(subPath2) == false)
            {
                fileSystem.CreateDirectory(subPath2);
            }

            if (fileSystem.FileExists(subFile1) == false)
            {
                using var stream =
                    fileSystem.OpenFile(subFile1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                stream.Write(contentBytes, 0, contentBytes.Length);
            }

            if (fileSystem.FileExists(subFile1_1) == false)
            {
                using var stream =
                    fileSystem.OpenFile(subFile1_1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                stream.Write(contentBytes, 0, contentBytes.Length);
            }

            var directoryEntries = fileSystem.EnumerateDirectoryEntries(subPath);
            foreach (var entry in directoryEntries)
            {
                Console.WriteLine(entry.Path);
            }

            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath2));
        }

        [TestMethod]
        public void 在資料夾建立檔案()
        {
            var       rootUPath  = CreateRootPath();
            using var fileSystem = new MemoryFileSystem();

            var subName      = "TestFolder";
            var subPath      = $"{rootUPath}/{subName}";
            var subPath1     = $"{subPath}/1";
            var subFile1     = $"{subPath}/1/1.txt";
            var subPath1_1   = $"{subPath}/1/1_1";
            var subFile1_1   = $"{subPath}/1/1_1/1_1.txt";
            var subPath1_1_1 = $"{subPath}/1/1_1/1_1_1";
            var subPath2     = $"{subPath}/2";
            var content      = "This is test string";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            if (fileSystem.DirectoryExists(subPath1_1_1) == false)
            {
                fileSystem.CreateDirectory(subPath1_1_1);
            }

            if (fileSystem.DirectoryExists(subPath2) == false)
            {
                fileSystem.CreateDirectory(subPath2);
            }

            if (fileSystem.FileExists(subFile1) == false)
            {
                using var stream =
                    fileSystem.OpenFile(subFile1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                stream.Write(contentBytes, 0, contentBytes.Length);
            }

            if (fileSystem.FileExists(subFile1_1) == false)
            {
                using var stream =
                    fileSystem.OpenFile(subFile1_1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                stream.Write(contentBytes, 0, contentBytes.Length);
            }

            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath2));
        }

        [TestMethod]
        public void 建立資料夾()
        {
            var       rootUPath  = CreateRootPath();
            using var fileSystem = new MemoryFileSystem();

            var subName      = "TestFolder";
            var subPath      = $"{rootUPath}/{subName}";
            var subPath1     = $"{subPath}/1";
            var subPath1_1   = $"{subPath}/1/1_1";
            var subPath1_1_1 = $"{subPath}/1/1_1/1_1_1";
            var subPath2     = $"{subPath}/2";
            var content      = "This is test string";

            if (fileSystem.DirectoryExists(subPath1_1_1) == false)
            {
                fileSystem.CreateDirectory(subPath1_1_1);
            }

            if (fileSystem.DirectoryExists(subPath2) == false)
            {
                fileSystem.CreateDirectory(subPath2);
            }

            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath2));
        }

        [TestMethod]
        public void 修改檔案日期()
        {
            var       rootUPath  = CreateRootPath();
            using var fileSystem = new MemoryFileSystem();

            var subName = "TestFolder";

            var subPath      = $"{rootUPath}/{subName}";
            var subPath1     = $"{subPath}/1";
            var subFile1     = $"{subPath}/1/1.txt";
            var subFile2     = $"{subPath}/1/2.txt";
            var subPath1_1   = $"{subPath}/1/1_1";
            var subFile1_1   = $"{subPath}/1/1_1/1_1.txt";
            var subPath1_1_1 = $"{subPath}/1/1_1/1_1_1";
            var subPath2     = $"{subPath}/2";
            var content      = "This is test string";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            if (fileSystem.DirectoryExists(subPath1_1_1) == false)
            {
                fileSystem.CreateDirectory(subPath1_1_1);
            }

            if (fileSystem.DirectoryExists(subPath2) == false)
            {
                fileSystem.CreateDirectory(subPath2);
            }

            if (fileSystem.FileExists(subFile1) == false)
            {
                using var stream =
                    fileSystem.OpenFile(subFile1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                stream.Write(contentBytes, 0, contentBytes.Length);
            }

            if (fileSystem.FileExists(subFile1_1) == false)
            {
                using var stream =
                    fileSystem.OpenFile(subFile1_1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                stream.Write(contentBytes, 0, contentBytes.Length);
            }

            var fileEntry = fileSystem.GetFileEntry(subFile1);
            fileEntry.CreationTime   = new DateTime(1900, 1, 1);
            fileEntry.LastWriteTime  = new DateTime(1900, 1, 2);
            fileEntry.LastAccessTime = new DateTime(1900, 1, 3);

            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath2));

            fileSystem.DeleteDirectory(subPath, true);
        }

        private static UPath CreateRootPath()
        {
            using var fileSystem        = new PhysicalFileSystem();
            var       executingAssembly = Assembly.GetExecutingAssembly();
            var       rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            return fileSystem.ConvertPathFromInternal(rootPath);
        }
    }
}