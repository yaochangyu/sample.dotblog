using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace UnitTestProject1
{
    public static class ValidationExtensions
    {
        public static bool TryValidate<T>(this T instance,
                                          ICollection<ValidationResult> validationResults)

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

        private static bool TryValidateObject<T>(T instance,
                                                 ICollection<ValidationResult> validationResults)
        {
            var isValid = false;

            var context = new ValidationContext(instance, null, null);
            isValid = Validator.TryValidateObject(instance, context,
                                                  validationResults, true);

            return isValid;
        }
    }
}