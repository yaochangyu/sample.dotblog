using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceStack.IO;

namespace Lab.ServiceStack.FVS.TestProject
{
    [TestClass]
    public class SurveyFileSystemVirtualFilesTests
    {
        [TestMethod]
        public void 新增資料夾()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";
            var subPath1          = $"{subPath}/1/1_1/1_1_1";
            var subPath2          = $"{subPath}/2";

            var virtualFiles = new FileSystemVirtualFiles(rootPath);
            if (virtualFiles.DirectoryExists(subPath1) == false)
            {
                virtualFiles.EnsureDirectory(subPath1);
            }

            if (virtualFiles.DirectoryExists(subPath2) == false)
            {
                virtualFiles.EnsureDirectory(subPath2);
            }

            virtualFiles.DeleteFolder(subPath);
        }

        [TestMethod]
        public void 新增檔案()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var rootPath          = Path.GetDirectoryName(executingAssembly.Location);
            var subPath           = "TestFolder";
            var content           = "This is test string";

            var virtualFiles = new FileSystemVirtualFiles(rootPath);
            if (virtualFiles.DirectoryExists(subPath) == false)
            {
                virtualFiles.EnsureDirectory(subPath);
            }

            virtualFiles.AppendFile($"{subPath}/1.txt", content);
        }
    }
}