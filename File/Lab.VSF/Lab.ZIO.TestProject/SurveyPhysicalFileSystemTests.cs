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
    public class SurveyPhysicalFileSystemTests
    {
        [TestMethod]
        public void 列舉根路徑內的子資料夾()
        {
            using var fileSystem        = new PhysicalFileSystem();
            var       executingAssembly = Assembly.GetExecutingAssembly();
            var       rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var       rootUPath         = fileSystem.ConvertPathFromInternal(rootPath);
            var       subName           = "TestFolder";

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

            var directoryEntries = fileSystem.EnumerateDirectoryEntries(subPath);
            foreach (var entry in directoryEntries)
            {
                Console.WriteLine(entry.Path);
            }

            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath1_1_1));
            Assert.AreEqual(true, fileSystem.DirectoryExists(subPath2));

            fileSystem.DeleteDirectory(subPath, true);
        }

        [TestMethod]
        public void 在資料夾建立檔案()
        {
            using var fileSystem        = new PhysicalFileSystem();
            var       executingAssembly = Assembly.GetExecutingAssembly();
            var       rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var       rootUPath         = fileSystem.ConvertPathFromInternal(rootPath);

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

            fileSystem.DeleteDirectory(subPath, true);
        }

        [TestMethod]
        public void 建立資料夾()
        {
            using var fileSystem        = new PhysicalFileSystem();
            var       executingAssembly = Assembly.GetExecutingAssembly();
            var       rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var       rootUPath         = fileSystem.ConvertPathFromInternal(rootPath);

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
            fileSystem.DeleteDirectory(subPath, true);
        }

        [TestMethod]
        public void 修改檔案日期()
        {
            using var fileSystem        = new PhysicalFileSystem();
            var       executingAssembly = Assembly.GetExecutingAssembly();
            var       rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var       rootUPath         = fileSystem.ConvertPathFromInternal(rootPath);
            var       subName           = "TestFolder";

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
    }
}