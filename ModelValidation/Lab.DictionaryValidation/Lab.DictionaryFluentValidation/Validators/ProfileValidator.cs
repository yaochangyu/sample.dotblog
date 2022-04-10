using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation.Fields;

namespace Lab.DictionaryFluentValidation.Validators;

public class ProfileValidator : AbstractValidator<Dictionary<string, object>>
{
    private static readonly Lazy<EmailFieldValidator> s_emailFieldValidatorLazy
        = new(() => new EmailFieldValidator());

    private static readonly Lazy<NameFieldValidator> s_nameFieldValidatorLazy
        = new(() => new NameFieldValidator());

    private static readonly Lazy<BirthdayFieldValidator> s_birthdayFieldValidatorLazy
        = new(() => new BirthdayFieldValidator());

    private static readonly Lazy<GenderFieldValidator> s_genderFieldValidatorLazy
        = new(() => new GenderFieldValidator());

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
        this.SetValidateRule(instances);

        return true;
    }

    private void SetValidateRule(Dictionary<string, object> instances)
    {
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
                   this.RuleFor(p => p[fieldName])
                        .SetValidator(p => s_emailFieldValidatorLazy.Value);
            
                    break;
                }
                case ProfileFieldNames.Name:
                {
                    this.RuleFor(p => p[fieldName])
                        .SetValidator(p => s_nameFieldValidatorLazy.Value)
                        ;
                    break;
                }
                case ProfileFieldNames.Birthday:
                {
                    this.RuleFor(p => p[fieldName])
                        .SetValidator(p => s_birthdayFieldValidatorLazy.Value)
                        ;
                    break;
                }
                case ProfileFieldNames.Gender:
                {
                    this.RuleFor(p => p[fieldName])
                        .SetValidator(p => s_genderFieldValidatorLazy.Value)
                        ;
                    break;
                }
            }
        }
    }
}