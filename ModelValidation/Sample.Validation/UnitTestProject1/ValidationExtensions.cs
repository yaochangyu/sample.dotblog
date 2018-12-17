using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace UnitTestProject1
{
    public static class ValidationExtensions
    {
        public static bool TryValidate<T>(T instance,
                                          ICollection<ValidationResult> validationResults)
            where T : class, new()
        {
            var items = instance as IEnumerable;
            if (items == null)
            {
                Validate(instance, validationResults);
            }
            else
            {
                foreach (var item in items)
                {
                    Validate(item, validationResults);
                }
            }

            return !validationResults.Any();
        }

        private static bool Validate<T>(T instance,
                                        ICollection<ValidationResult> validationResults)
        {
            var isValid = false;

            var context = new ValidationContext(instance, null, null);
            validationResults = new List<ValidationResult>();

            isValid = Validator.TryValidateObject(instance, context,
                                                  validationResults, true);

            return isValid;
        }
    }
}