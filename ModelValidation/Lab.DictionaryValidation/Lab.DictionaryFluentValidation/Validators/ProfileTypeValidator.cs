using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation.Fields;

namespace Lab.DictionaryFluentValidation.Validators;

public class ProfileTypeValidator : AbstractValidator<Dictionary<string, object>>
{
    private static EmailTypeValidator EmailTypeValidator => new(ProfileTypeNames.ContactEmail);

    private static NameTypeValidator NameTypeValidator => new(ProfileTypeNames.Name);

    private static BirthdayTypeValidator BirthdayTypeValidator => new(ProfileTypeNames.Birthday);

    private static GenderTypeValidator GenderTypeValidator => new(ProfileTypeNames.Gender);

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
                case ProfileTypeNames.Name:
                    isNotSupports.Add(IsNotSupportNestFields(NameTypeNames.GetFieldNames(), fieldValue, context));
                    break;
                case ProfileTypeNames.Birthday:
                    isNotSupports.Add(IsNotSupportNestFields(BirthdayTypeNames.GetFieldNames(), fieldValue, context));
                    break;
                default:
                    isNotSupports.Add(IsNotSupportFields(ProfileTypeNames.GetFieldNames(), fieldName, context));
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
                case ProfileTypeNames.ContactEmail:
                {
                    var PropertyName = fieldName;
                    this.RuleFor(p => p[fieldName])
                        .SetValidator(p => EmailTypeValidator)
                        ;

                    break;
                }
                case ProfileTypeNames.Name:
                {
                    this.RuleFor(p => p[fieldName])
                        .SetValidator(p => NameTypeValidator)
                        ;
                    break;
                }
                case ProfileTypeNames.Birthday:
                {
                    this.RuleFor(p => p[fieldName])
                        .SetValidator(p => BirthdayTypeValidator)
                        ;
                    break;
                }
                case ProfileTypeNames.Gender:
                {
                    this.RuleFor(p => p[fieldName])
                        .SetValidator(p => GenderTypeValidator)
                        ;
                    break;
                }
            }
        }
    }
}