using System.ComponentModel.DataAnnotations;

namespace Lab.LineBot.SDK.Models
{
    public class TokenRequest
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string ClientId { get; set; }

        [Required]
        public string ClientSecret { get; set; }

        [Required]
        public string CallbackUrl { get; set; }
    }
}