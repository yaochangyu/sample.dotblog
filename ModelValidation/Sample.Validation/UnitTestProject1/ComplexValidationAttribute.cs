using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace UnitTestProject1
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ComplexValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object entity, ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            if (entity == null)
            {
                return ValidationResult.Success;
            }

            var displayName = validationContext.DisplayName;
            var compositeResults = new CompositeValidationResult($"{displayName} validate failed!");

            var items = entity as IEnumerable;

            if (items != null)
            {
                var index = 0;
                foreach (var item in items)
                {
                    var validationResults = new List<ValidationResult>();

                    var context = new ValidationContext(item, null, null);
                    Validator.TryValidateObject(item, context, validationResults, true);

                    validationResults.ForEach(x => compositeResults.Add(x, displayName, index));
                    index++;
                }
            }
            else
            {
                var validationResults = new List<ValidationResult>();

                var context = new ValidationContext(entity, null, null);
                Validator.TryValidateObject(entity, context, validationResults, true);

                validationResults.ForEach(p => compositeResults.Add(p, displayName));
            }

            return compositeResults.ValidationResults.Any() ? compositeResults : ValidationResult.Success;
        }
    }
}