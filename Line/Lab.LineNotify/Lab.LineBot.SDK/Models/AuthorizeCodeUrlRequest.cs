using System.ComponentModel.DataAnnotations;

namespace Lab.LineBot.SDK.Models
{
    public class AuthorizeCodeUrlRequest
    {
        [Required]
        public string CallbackUrl { get; set; }

        [Required]
        public string ClientId { get; set; }

        [Required]
        public string State { get; set; }
    }
}