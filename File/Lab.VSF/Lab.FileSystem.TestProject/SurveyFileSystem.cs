using System;
using System.IO;
using System.Reflection;
using System.Text;
using Lexical.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.FileSystem.TestProject
{
    [TestClass]
    public class SurveyFileSystem
    {
        [TestMethod]
        public void 列舉根路徑內的所有結構()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";
            var subPath1          = $"{subPath}/1";
            var subPath1_1        = $"{subPath}/1/1_1";
            var subPath1_1_1      = $"{subPath}/1/1_1/1_1_1";
            var subPath2          = $"{subPath}/2";
            var content           = "This is test string";
            var contentBytes      = Encoding.UTF8.GetBytes(content);

            Lexical.FileSystem.FileSystem fileSystem = null;
            try
            {
                fileSystem = new Lexical.FileSystem.FileSystem(rootPath);
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

                var lines = fileSystem.VisitTree(subPath);
                foreach (var line in lines)
                {
                    Console.WriteLine($"{line.Path}");
                }
                
                fileSystem.PrintTo(Console.Out, subPath);
            }
            finally
            {
                //還原 
                fileSystem.Delete(subPath, true);
            }
        }

        [TestMethod]
        public void 列舉根路徑內的子資料夾()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";
            var subPath1          = $"{subPath}/1";
            var subPath1_1        = $"{subPath}/1/1_1";
            var subPath1_1_1      = $"{subPath}/1/1_1/1_1_1";
            var subPath2          = $"{subPath}/2";
            var content           = "This is test string";
            var contentBytes      = Encoding.UTF8.GetBytes(content);

            Lexical.FileSystem.FileSystem fileSystem = null;
            try
            {
                fileSystem = new Lexical.FileSystem.FileSystem(rootPath);
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

                fileSystem.PrintTo(Console.Out, subPath);

                foreach (var entry in fileSystem.Browse(subPath))
                {
                    var path = entry.Path;
                    Console.WriteLine(path);
                }
            }
            finally
            {
                //還原 
                fileSystem.Delete(subPath, true);
            }
        }

        [TestMethod]
        public void 在資料夾建立檔案()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";
            var subPath1          = $"{subPath}/1";
            var subPath1_1        = $"{subPath}/1/1_1";
            var subPath1_1_1      = $"{subPath}/1/1_1/1_1_1";
            var subPath2          = $"{subPath}/2";
            var content           = "This is test string";
            var contentBytes      = Encoding.UTF8.GetBytes(content);

            Lexical.FileSystem.FileSystem fileSystem = null;
            try
            {
                fileSystem = new Lexical.FileSystem.FileSystem(rootPath);
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

                fileSystem.PrintTo(Console.Out, subPath);
            }
            finally
            {
                //還原 
                fileSystem.Delete(subPath, true);
            }
        }

        [TestMethod]
        public void 建立資料夾()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";
            var subPath1          = $"{subPath}/1";
            var subPath1_1        = $"{subPath}/1/1_1";
            var subPath1_1_1      = $"{subPath}/1/1_1/1_1_1";
            var subPath2          = $"{subPath}/2";
            var content           = "This is test string";

            Lexical.FileSystem.FileSystem fileSystem = null;
            try
            {
                fileSystem = new Lexical.FileSystem.FileSystem(rootPath);
                if (fileSystem.Exists(subPath1_1_1) == false)
                {
                    fileSystem.CreateDirectory(subPath1_1_1);
                }

                if (fileSystem.Exists(subPath1_1) == false)
                {
                    fileSystem.CreateDirectory(subPath1_1);
                }

                if (fileSystem.Exists(subPath1) == false)
                {
                    fileSystem.CreateDirectory(subPath1);
                }

                if (fileSystem.Exists(subPath2) == false)
                {
                    fileSystem.CreateDirectory(subPath2);
                }

                fileSystem.PrintTo(Console.Out, subPath);
            }
            finally
            {
                //還原 
                fileSystem.Delete(subPath, true);
            }
        }
    }
}