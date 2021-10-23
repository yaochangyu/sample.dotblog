using System.IO;

namespace Lab.Test.WebApi.Net5
{
    public class FileProvider:IFileProvider
    {
        public string Name()
        {
            return nameof(FileProvider);
        }
    }
}