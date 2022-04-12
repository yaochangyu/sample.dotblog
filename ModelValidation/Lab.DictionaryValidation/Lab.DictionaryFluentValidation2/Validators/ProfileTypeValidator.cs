using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation2.Fields;

namespace Lab.DictionaryFluentValidation2.Validators;

public class ProfileTypeValidator : AbstractValidator<Dictionary<string, object>>
{
    private static readonly Lazy<EmailTypeValidator> s_emailTypeValidatorLazy =
        new(() => new EmailTypeValidator(ProfileTypeNames.ContactEmail));

    private static readonly Lazy<NameTypeValidator> s_nameTypeValidator =
        new Lazy<NameTypeValidator>(() => new NameTypeValidator(ProfileTypeNames.Name));

    private static readonly Lazy<BirthdayTypeValidator2> s_birthdayTypeValidatorLazy =
        new(() => new BirthdayTypeValidator2(ProfileTypeNames.Birthday));

    private static readonly Lazy<GenderTypeValidator> s_genderTypeValidatorLazy =
        new(() => new GenderTypeValidator(ProfileTypeNames.Gender));

    private static EmailTypeValidator EmailTypeValidator => s_emailTypeValidatorLazy.Value;

    private static NameTypeValidator NameTypeValidator => s_nameTypeValidator.Value;

    private static BirthdayTypeValidator2 BirthdayTypeValidator => s_birthdayTypeValidatorLazy.Value;

    private static GenderTypeValidator GenderTypeValidator => s_genderTypeValidatorLazy.Value;

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
                    isNotSupports.Add(IsNotSupportNestFields(BirthdayType.GetFieldNames(), fieldValue, context));
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
        var isNotSupport = sourceFields.ContainsKey(destFieldName) == false;
        if (isNotSupport)
        {
            var failure = new ValidationFailure(destFieldName,
                                                $"'{destFieldName}' column not support")
            {
                ErrorCode = "NotSupportValidator",
            };
            context.AddFailure(failure);
        }

        return isNotSupport;
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
            // return true;
        }

        var instances = context.InstanceToValidate;
        this.SetValidateRule(context, instances);

        return true;
    }

    private void SetValidateRule(ValidationContext<Dictionary<string, object>> validationContext,
                                 Dictionary<string, object> instances)
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
                    var birthdayType = new BirthdayType
                    {
                        // Year = 1900,
                        Month = 1,
                        Day = 1
                    };

                    var validationResult = BirthdayTypeValidator.Validate(birthdayType);
                    if (validationResult.IsValid == false)
                    {
                        foreach (var validationFailure in validationResult.Errors)
                        {
                            validationContext.AddFailure(validationFailure);
                        }
                    }

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