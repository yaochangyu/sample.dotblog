using System.ComponentModel.DataAnnotations;

namespace Lab.LineBot.SDK.Models
{
    public class NotifyWithImageRequest
    {
        [Required]
        public string Message { get; set; }

        [Required]
        public string AccessToken { get; set; }

        public string FilePath { get; set; }

        public byte[] FileBytes { get; set; }
    }
}