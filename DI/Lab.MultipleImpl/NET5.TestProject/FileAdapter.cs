using NET5.TestProject.File;

namespace NET5.TestProject
{
    public class FileAdapter
    {
        private readonly IFileProvider _fileProvider;

        public FileAdapter(IFileProvider fileProvider)
        {
            this._fileProvider = fileProvider;
        }

        public string Get()
        {
            return this._fileProvider.Print();
        }
    }
}