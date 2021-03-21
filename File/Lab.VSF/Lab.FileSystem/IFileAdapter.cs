using System.Collections.Generic;

namespace Lab.FileSystem
{
    public interface IFileAdapter
    {
        void DeleteAgo(string folderPath, int day);

        Dictionary<string, string> GetContents(string folderPath);

        ICollection<string> GetFileNames(string folderPath);
    }
}