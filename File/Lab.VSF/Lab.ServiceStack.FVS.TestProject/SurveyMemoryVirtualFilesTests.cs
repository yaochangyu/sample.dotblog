using System;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.IO;

namespace Lab.ServiceStack.FVS.TestProject
{
    [TestClass]
    public class SurveyMemoryVirtualFilesTests
    {
        [TestMethod]
        public void 新增資料夾()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var content           = "This is test string";
            var subPath           = "TestFolder";
            var subPath1          = $"{subPath}/1/1_1/1_1_1";
            var subPath2          = $"{subPath}/2";
            var fileSystem        = new FileSystemVirtualFiles(rootPath);
            fileSystem.EnsureDirectory(subPath1);
            fileSystem.EnsureDirectory(subPath2);
            fileSystem.AppendFile($"{subPath}/1.txt", content);
            var memoryFileSystem = new MemoryVirtualFiles();

            // var memoryFileSystem1 = fileSystem.GetMemoryVirtualFiles();

            // var nonDefaultValues = fileSystem.PopulateWithNonDefaultValues(memoryFileSystem);
            // var memoryFileSystem2 = memoryFileSystem.PopulateWith(fileSystem);

            var subFolder = new InMemoryVirtualDirectory(memoryFileSystem, subPath);
            var subFile   = new InMemoryVirtualFile(memoryFileSystem, subFolder);
            memoryFileSystem.AddFile(subFile);

            //無法單獨加入資料夾
            var subFolder1 = new InMemoryVirtualDirectory(memoryFileSystem, "1", subFolder);

            var subFolder2 = new InMemoryVirtualDirectory(memoryFileSystem, "1_1", subFolder1);
            subFolder2.AddFile("2.txt", content);

            var directories = memoryFileSystem.RootDirectory.Directories;
            var files       = memoryFileSystem.Files;
            Console.WriteLine();

            //
            // // memorySystem.AddFile(new InMemoryVirtualFile(fileSystem, directory));
            //
            // //            memorySystem.AppendFile($"{subPath1}/1.txt",content);
            // var files = memoryFileSystem.GetAllFiles();
        }
    }
}