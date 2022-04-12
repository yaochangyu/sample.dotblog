using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation2.Fields;

namespace Lab.DictionaryFluentValidation2.Validators;

public class BirthdayTypeValidator2 : AbstractValidator<BirthdayType>
{
    private readonly string _propertyName;
    private const string ErrorCode = nameof(BirthdayTypeValidator2);

    public BirthdayTypeValidator2(string propertyName)
    {
        this._propertyName = propertyName;
    }

    /// <summary>
    ///     return true 繼續往下驗證
    ///     https://docs.fluentvalidation.net/en/latest/advanced.html?highlight=PreValidate#prevalidate
    /// </summary>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    protected override bool PreValidate(ValidationContext<BirthdayType> context, ValidationResult result)
    {
        var instance = context.InstanceToValidate;
        if (instance == null)
        {
            return true;
        }
        try
        {
            var validDate = new DateTime(instance.Year, instance.Month, instance.Day);
        }
        catch (Exception e)
        {
            var errorMsg = $"year:{instance.Year}," +
                           $"month:{instance.Month}," +
                           $"day:{instance.Day} is invalid date format";

            var validationFailure = new ValidationFailure(this._propertyName, errorMsg)
            {
                ErrorCode = ErrorCode
            };
            context.AddFailure(validationFailure);
        }

        return true;
    }
}

public class BirthdayTypeValidator : AbstractValidator<object>
{
    private const string ErrorCode = nameof(BirthdayTypeValidator);
    private readonly string _propertyName;

    public BirthdayTypeValidator(string propertyName)
    {
        this._propertyName = propertyName;
    }

    private bool HasRequireField(ValidationContext<object> context, Dictionary<string, string> srcBirthdayFields,
                                 Dictionary<string, int> destBirthdayFields)
    {
        var isValid = true;
        foreach (var srcField in srcBirthdayFields)
        {
            var srcKey = srcField.Key;
            if (destBirthdayFields.ContainsKey(srcKey) == false)
            {
                var propertyName = $"{this._propertyName}.{srcKey}";
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

        return isValid;
    }
}