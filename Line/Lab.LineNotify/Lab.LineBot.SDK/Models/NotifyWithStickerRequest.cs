using System.ComponentModel.DataAnnotations;

namespace Lab.LineBot.SDK.Models
{
    public class NotifyWithStickerRequest
    {
        [Required]
        public string Message { get; set; }

        [Required]
        public string AccessToken { get; set; }

        //https://developers.line.biz/en/docs/messaging-api/sticker-list/#sticker-definitions
        public string StickerPackageId { get; set; }

        public string StickerId { get; set; }
    }
}