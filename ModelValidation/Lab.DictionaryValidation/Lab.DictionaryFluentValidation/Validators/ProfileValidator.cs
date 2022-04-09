using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation.Fields;

namespace Lab.DictionaryFluentValidation.Validators;

public class ProfileValidator : AbstractValidator<Dictionary<string, object>>
{
    private static readonly Lazy<EmailFieldValidator> s_emailFieldValidatorLazy = new(() => new EmailFieldValidator());

    private static readonly Lazy<RequireFieldValidator> s_requireFieldValidatorLazy =
        new(() => new RequireFieldValidator());

    private static bool IsNotSupportFields(ValidationContext<Dictionary<string, object>> context)
    {
        var instances = context.InstanceToValidate;
        var isNotSupports = new List<bool>();
        foreach (var item in instances)
        {
            var fieldName = item.Key;
            var fieldValue = item.Value;

            switch (fieldName)
            {
                case ProfileFieldNames.Name:
                    isNotSupports.Add(IsNotSupportNestFields(NameFieldNames.GetFields(), fieldValue, context));
                    break;
                case ProfileFieldNames.Birthday:
                    isNotSupports.Add(IsNotSupportNestFields(BirthdayFieldNames.GetFields(), fieldValue, context));
                    break;
                default:
                    isNotSupports.Add(IsNotSupportFields(ProfileFieldNames.GetFields(), fieldName, context));
                    break;
            }
        }

        return isNotSupports.Any(p => p);
    }

    private static bool IsNotSupportFields(Dictionary<string, string> sourceFields,
                                           string destFieldName,
                                           ValidationContext<Dictionary<string, object>> context)
    {
        var isSNotSupport = sourceFields.ContainsKey(destFieldName) == false;
        if (isSNotSupport)
        {
            var failure = new ValidationFailure(destFieldName,
                                                $"not support column '{destFieldName}'")
            {
                ErrorCode = "NotSupportValidator",
            };
            context.AddFailure(failure);
        }

        return isSNotSupport;
    }

    private static bool IsNotSupportNestFields(Dictionary<string, string> sourceFields,
                                               object destValue,
                                               ValidationContext<Dictionary<string, object>> context)
    {
        if (destValue == null)
        {
            return false;
        }

        var isNotSupports = new List<bool>();

        var propertyInfos = destValue.GetType().GetProperties();
        foreach (var propertyInfo in propertyInfos)
        {
            isNotSupports.Add(IsNotSupportFields(sourceFields, propertyInfo.Name, context));
        }

        return isNotSupports.Any(p => p);
    }

    protected override bool PreValidate(ValidationContext<Dictionary<string, object>> context, ValidationResult result)
    {
        if (IsNotSupportFields(context))
        {
            return false;
        }

        var instances = context.InstanceToValidate;
        foreach (var item in instances)
        {
            var fieldName = item.Key;
            var fieldValue = item.Value;
            if (fieldValue == null)
            {
                continue;
            }

            switch (fieldName)
            {
                case ProfileFieldNames.ContactEmail:
                {
                    ValidateEmail(fieldValue.ToString(), context);
                    break;
                }
                case ProfileFieldNames.Name:
                {
                    this.ValidateName(fieldName, fieldValue, context);
                    break;
                }
            }
        }

        return true;
    }

    private static void ValidateEmail(string value, ValidationContext<Dictionary<string, object>> context)
    {
        var validationResult = s_emailFieldValidatorLazy.Value.Validate(value);
        ValidationContextAddFailure(ProfileFieldNames.ContactEmail, validationResult, context);
    }

    private void ValidateName(string fieldName,
                              object fieldValue, ValidationContext<Dictionary<string, object>> context)
    {
        var propertyInfos = fieldValue.GetType().GetProperties();
        foreach (var propertyInfo in propertyInfos)
        {
            var value = propertyInfo.GetValue(fieldValue);
            switch (propertyInfo.Name)
            {
                case NameFieldNames.FullName:
                    ValidateRequireField(context, $"{fieldName}.{NameFieldNames.FullName}", value);
                    break;
            }
        }
    }

    private static void ValidateRequireField(ValidationContext<Dictionary<string, object>> context,
                                             string fieldName,
                                             object fieldValue)
    {
        var validationResult = s_requireFieldValidatorLazy.Value.Validate(fieldValue);
        ValidationContextAddFailure(fieldName, validationResult, context);
    }

    private static void ValidationContextAddFailure(string fieldName,
                                                    ValidationResult validationResult,
                                                    ValidationContext<Dictionary<string, object>> context)
    {
        if (validationResult.IsValid)
        {
            return;
        }

        foreach (var error in validationResult.Errors)
        {
            var failure = new ValidationFailure(fieldName,
                                                error.ErrorMessage,
                                                error.AttemptedValue)
            {
                ErrorCode = error.ErrorCode,
            };
            context.AddFailure(failure);
        }
    }
}