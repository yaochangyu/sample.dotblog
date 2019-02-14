using System.Collections.Generic;

namespace Server.Models
{
    public class UploadResponse
    {
        public UploadResponse()
        {
            this.FileNames = new List<string>();
            this.ContentTypes = new List<string>();
            this.Names = new List<string>();
        }

        public ICollection<string> FileNames { get; set; }

        public string Description { get; set; }
        public string DownloadLink { get; set; }

        public ICollection<string> ContentTypes { get; set; }

        public ICollection<string> Names { get; set; }
    }
}