using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation.Fields;

namespace Lab.DictionaryFluentValidation.Validators;

public class ProfileValidator : AbstractValidator<Dictionary<string, object>>
{
    private readonly Lazy<EmailFieldValidator> _emailFieldValidatorLazy = new(new EmailFieldValidator());

    public ProfileValidator()
    {
        this._emailFieldValidatorLazy = new(new EmailFieldValidator());
    }

    protected override bool PreValidate(ValidationContext<Dictionary<string, object>> context, ValidationResult result)
    {
        if (ValidateSupportFields(context) == false)
        {
            return false;
        }

        var instances = context.InstanceToValidate;
        foreach (var item in instances)
        {
            switch (item.Key)
            {
                case ProfileFieldNames.ContactEmail:
                {
                    var validationResult = this._emailFieldValidatorLazy.Value.Validate(item.Value.ToString());
                    if (validationResult.IsValid == false)
                    {
                        foreach (var error in validationResult.Errors)
                        {
                            var failure = new ValidationFailure(ProfileFieldNames.ContactEmail,
                                                                error.ErrorMessage,
                                                                error.AttemptedValue)
                            {
                                ErrorCode = error.ErrorCode,
                            };
                            context.AddFailure(failure);
                        }
                    }

                    break;
                }
            }
        }

        return true;
    }

    private static bool ValidateSupportFields(ValidationContext<Dictionary<string, object>> context)
    {
        var instances = context.InstanceToValidate;
        var isValid = true;
        foreach (var item in instances)
        {
            var fieldName = item.Key;
            var fieldValue = item.Value;
            switch (fieldName)
            {
                case ProfileFieldNames.Name:
                {
                    var propertyInfos = fieldValue.GetType().GetProperties();
                    foreach (var propertyInfo in propertyInfos)
                    {
                        isValid = IsSupportFields(context, NameFieldNames.GetFields(), propertyInfo.Name);
                    }

                    break;
                }
                default:
                {
                    isValid = IsSupportFields(context, ProfileFieldNames.GetFields(), fieldName);
                    break;
                }
            }
        }

        return isValid;
    }

    private static bool IsSupportFields(ValidationContext<Dictionary<string, object>> context,
                                        Dictionary<string, string> sourceFields,
                                        string destFieldName)
    {
        var notExistKey = sourceFields.ContainsKey(destFieldName) == false;
        if (notExistKey)
        {
            context.AddFailure(destFieldName, $"not support column '{destFieldName}'");
            return false;
        }

        return true;
    }
}