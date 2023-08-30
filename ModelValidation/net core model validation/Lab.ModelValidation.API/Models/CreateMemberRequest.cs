using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Lab.ModelValidation.API.Models;

public class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberRequestValidator()
    {
        this.RuleFor(p => p.Name).NotNull().NotEmpty();
        this.RuleFor(p => p.Age).LessThanOrEqualTo(18).GreaterThan(200);
        this.RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type is not valid");
    }
}

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
}