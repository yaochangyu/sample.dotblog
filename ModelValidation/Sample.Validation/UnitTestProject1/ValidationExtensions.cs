using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace UnitTestProject1
{
    public static class ValidationExtensions
    {
        public static bool TryValidate<T>(this T instance, ICollection<ValidationResult> validationResults)
            where T : class, new()
        {
            var items = instance as IEnumerable;
            if (items == null)
            {
                TryValidateObject(instance, validationResults);
            }
            else
            {
                foreach (var item in items)
                {
                    TryValidateObject(item, validationResults);
                }
            }

            return !validationResults.Any();
        }

        private static bool TryValidateObject<T>(T instance, ICollection<ValidationResult> validationResults)
        {
            var isValid = false;

            var context = new ValidationContext(instance, null, null);
            isValid = Validator.TryValidateObject(instance, context, validationResults, true);
            return isValid;
        }

        public static bool TryValidate2<T>(this T instance, List<ValidationResult> validationResults)
            where T : class, new()
        {
            var items = instance as IEnumerable;
            if (items == null)
            {
                TryValidateObject2(instance, validationResults);
            }
            else
            {
                foreach (var item in items)
                {
                    TryValidateObject2(item, validationResults);
                }
            }

            return !validationResults.Any();
        }

        private static bool TryValidateObject2<T>(T instance, List<ValidationResult> validationResults)
        {
            var isValid = false;

            var context = new ValidationContext(instance, null, null);
            var errors = new List<ValidationResult>();
            isValid = Validator.TryValidateObject(instance, context, errors, true);
            foreach (var error in errors)
            {
                if (error is CompositeValidationResult)
                {
                    var result = error as CompositeValidationResult;
                    validationResults.AddRange(result.ValidationResults);
                }
                else
                {
                    validationResults.Add(error);
                }
            }

            return isValid;
        }
    }
}