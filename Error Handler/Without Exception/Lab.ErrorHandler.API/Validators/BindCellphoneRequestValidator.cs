using FluentValidation;
using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API.Validators;

public class BindCellphoneRequestValidator : AbstractValidator<BindCellphoneRequest>
{
    public BindCellphoneRequestValidator()
    {
        this.RuleFor(p => p.Cellphone).NotNull().NotEmpty();
    }
}