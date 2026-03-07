using System.ComponentModel.DataAnnotations;

namespace IdempotencyKey.WebApi.Members;

public record UpdateMemberRequest(
    [Required, MaxLength(100)] string Name,
    [Required, EmailAddress, MaxLength(254)] string Email
);
