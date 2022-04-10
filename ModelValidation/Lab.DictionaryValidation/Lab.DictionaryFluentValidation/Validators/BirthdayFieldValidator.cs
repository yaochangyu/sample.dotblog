using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation.Fields;

namespace Lab.DictionaryFluentValidation.Validators;

public class BirthdayFieldValidator : AbstractValidator<object>
{
    private const string ErrorCode = nameof(BirthdayFieldValidator);

    /// <summary>
    ///     return true 繼續往下驗證
    ///     https://docs.fluentvalidation.net/en/latest/advanced.html?highlight=PreValidate#prevalidate
    /// </summary>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    protected override bool PreValidate(ValidationContext<object> context, ValidationResult result)
    {
        var isValid = true;
        var instance = context.InstanceToValidate;
        if (instance == null)
        {
            return isValid;
        }

        var propertyInfos = instance.GetType().GetProperties();
        var birthday = new Dictionary<string, int>();
        foreach (var propertyInfo in propertyInfos)
        {
            var value = propertyInfo.GetValue(instance);
            if (value == null)
            {
                continue;
            }

            birthday.Add(propertyInfo.Name, Convert.ToInt32(value));
        }

        var srcBirthdayFields = BirthdayFieldNames.GetFields();
        isValid = HasRequireField(context, srcBirthdayFields, birthday);
        if (isValid == false)
        {
            return isValid;
        }

        var year = birthday[BirthdayFieldNames.Year];
        var month = birthday[BirthdayFieldNames.Month];
        var day = birthday[BirthdayFieldNames.Day];
        try
        {
            var birthday2 = new DateTime(year, month, day);
        }
        catch (Exception e)
        {
            var errorMsg = $"{BirthdayFieldNames.Year}:{year}," +
                           $"{BirthdayFieldNames.Month}:{month}," +
                           $"{BirthdayFieldNames.Day}:{day} is invalid date format";

            var validationFailure = new ValidationFailure(ProfileFieldNames.Birthday, errorMsg)
            {
                ErrorCode = ErrorCode
            };
            context.AddFailure(validationFailure);
        }

        return isValid;
    }

    private static bool HasRequireField(ValidationContext<object> context, Dictionary<string, string> srcBirthdayFields,
                                        Dictionary<string, int> destBirthdayFields)
    {
        var isValid = true;
        foreach (var srcField in srcBirthdayFields)
        {
            var srcKey = srcField.Key;
            if (destBirthdayFields.ContainsKey(srcKey) == false)
            {
                var propertyName = $"birthday.{srcKey}";
                var validationFailure = new ValidationFailure(propertyName, $"'{propertyName}' must not be empty.")
                {
                    ErrorCode = ErrorCode
                };
                context.AddFailure(validationFailure);
                isValid = false;
            }
        }

        return isValid;
    }
}