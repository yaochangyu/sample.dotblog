using System;
using System.IO;
using System.Reflection;
using System.Text;
using Lexical.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.FileSystem.TestProject
{
    [TestClass]
    public class SurveyVirtualFileSystem
    {
        [TestMethod]
        public void Mount()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";

            using var fileSystem        = CreateFolder(rootPath, subPath);
            using var virtualFileSystem = new VirtualFileSystem();
            using var memoryFileSystem  = new MemoryFileSystem();

            Console.WriteLine("掛載到虛擬結構...");
            var appDir = rootPath.Replace('\\', '/');

            // virtualFileSystem.Mount("", new Lexical.FileSystem.FileSystem(appDir), Option.SubPath(appDir));
            virtualFileSystem.Mount("", Lexical.FileSystem.FileSystem.OS, Option.SubPath(appDir));

            //操作會對應到真實檔案
            virtualFileSystem.CreateDirectory($"/{subPath}/AAA");
            Console.WriteLine("virtualFileSystem");
            virtualFileSystem.PrintTo(Console.Out);
        }

        [TestMethod]
        public void 映射資料結構()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";

            var fileSystem        = CreateFolder(rootPath, subPath);
            var virtualFileSystem = new VirtualFileSystem();
            var memoryFileSystem  = new MemoryFileSystem();

            var appDir = rootPath.Replace('\\', '/');
            virtualFileSystem.Mount("", Lexical.FileSystem.FileSystem.OS, Option.SubPath(appDir));
            virtualFileSystem.CopyTree($"/{subPath}/", memoryFileSystem, "");
            memoryFileSystem.CreateDirectory("AAA");
            Console.WriteLine("memoryFileSystem");
            memoryFileSystem.PrintTo(Console.Out);
        }

        [TestMethod]
        public void 映射資料結構2()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";

            using var fileSystem       = CreateFolder(rootPath, subPath);
            using var memoryFileSystem = new MemoryFileSystem();

            foreach (var line in fileSystem.VisitTree(subPath))
            {
                if (line.Entry.IsDirectory())
                {
                    memoryFileSystem.CreateDirectory(line.Path);
                }

                if (line.Entry.IsFile())
                {
                    fileSystem.CopyFile(line.Path, memoryFileSystem, line.Path);
                }
            }

            memoryFileSystem.CreateDirectory("AAA");
            memoryFileSystem.PrintTo(Console.Out);
        }

        private static Lexical.FileSystem.FileSystem CreateFolder(string rootPath, string subPath)
        {
            var subPath1     = $"{subPath}/1";
            var subPath1_1   = $"{subPath}/1/1_1";
            var subPath1_1_1 = $"{subPath}/1/1_1/1_1_1";
            var subPath2     = $"{subPath}/2";
            var content      = "This is test string";
            var contentBytes = Encoding.UTF8.GetBytes(content);

            var fileSystem = new Lexical.FileSystem.FileSystem(rootPath);
            if (fileSystem.Exists(subPath1_1_1) == false)
            {
                fileSystem.CreateDirectory(subPath1_1_1);
            }

            if (fileSystem.Exists(subPath2) == false)
            {
                fileSystem.CreateDirectory(subPath2);
            }

            var file1 = $"{subPath1_1}/1_1.text";
            if (fileSystem.Exists(file1) == false)
            {
                fileSystem.CreateFile(file1, contentBytes);
            }

            var file2 = Path.Combine(rootPath, subPath1_1_1, "1_1_1.txt");
            if (fileSystem.Exists(file2) == false)
            {
                using var stream =
                    fileSystem.Open(file2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                stream.Write(contentBytes, 0, contentBytes.Length);
            }

            return fileSystem;
        }
    }
}