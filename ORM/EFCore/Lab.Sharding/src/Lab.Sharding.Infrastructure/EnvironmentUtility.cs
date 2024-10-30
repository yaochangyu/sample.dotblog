using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lab.Sharding.Infrastructure
{
    public static class EnvironmentUtility
    {
        public static string FindParentFolder(string parentFolderName)
        {
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var directory = new DirectoryInfo(currentDirectory);

            while (directory != null)
            {
                var folders = Directory.GetDirectories(directory.FullName, parentFolderName);
                if (folders.Length > 0)
                {
                    return folders[0];
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException($"Folder '{parentFolderName}' not found.");
        }

        public static void ReadEnvironmentFile(string folder, string fileName)
        {
            var fileFullPath = Path.Combine(folder, fileName);
            if (!File.Exists(fileFullPath))
            {
                throw new FileNotFoundException($"File '{fileName}' not found.");
            }

            //讀取 .env 檔案
            var lines = File.ReadAllLines(fileFullPath);
            foreach (var line in lines)
            {
                //排除註解
                if (line.StartsWith("#"))
                {
                    continue;
                }

                var parts = line.Split('=');
                if (parts.Length < 2)
                {
                    continue;
                }

                var key = parts[0];
                var value = "";
                for (int i = 1; i < parts.Length; i++)
                {
                    if (i == 1)
                    {
                        value += parts[i];
                        continue;
                    }

                    value += $"={parts[i]}";
                }

                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}