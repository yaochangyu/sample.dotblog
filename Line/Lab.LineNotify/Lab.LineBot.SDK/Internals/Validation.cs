using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Lab.LineBot.SDK.Internals
{
    public class Validation
    {
        public static bool TryValidate(object contact, out List<ValidationResult> errors)
        {
            var context = new ValidationContext(contact, null, null);
            errors = new List<ValidationResult>();
            return Validator.TryValidateObject(contact, context, errors, true);
        }

        public static void Validate(object instance)
        {
            var context = new ValidationContext(instance, null, null);
            Validator.ValidateObject(instance, context, true);
        }
    }
}