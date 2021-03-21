using System.Collections.Generic;

namespace Lab.FileSystem
{
    public interface IFileAdapter
    {
        void DeleteAgo(string folderName, int day);

        Dictionary<string, string> GetContents(string folderName);

        ICollection<string> GetFileNames(string folderName);
    }
}