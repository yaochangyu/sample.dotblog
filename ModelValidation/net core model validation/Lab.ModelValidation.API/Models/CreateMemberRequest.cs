using System.ComponentModel.DataAnnotations;
using Lab.ModelValidation.API.Controllers;

namespace Lab.ModelValidation.API.Models;

public enum MemberType
{
    None,
    Member,
    Vip,
}

public class CreateMemberRequest
{
    [Required]
    public string Name { get; set; }

    [Range(18, 200)]
    public int Age { get; set; }

    [Required]
    public MemberType Type { get; set; }
    public MemberType Type1 { get; set; }
}