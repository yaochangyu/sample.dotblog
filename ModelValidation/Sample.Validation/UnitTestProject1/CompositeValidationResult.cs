using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace UnitTestProject1
{
    public class CompositeValidationResult : ValidationResult
    {
        private readonly List<ValidationResult> _validationResults;

        public CompositeValidationResult(string errorMessage)
            : base(errorMessage)
        {
            if (this._validationResults == null)
            {
                this._validationResults = new List<ValidationResult>();
            }
        }

        public IEnumerable<ValidationResult> ValidationResults
        {
            get { return this._validationResults; }
        }

        public void Add(ValidationResult validationResult, string displayName)
        {
            if (validationResult == null)
            {
                throw new ArgumentNullException(nameof(validationResult));
            }

            var fieldName = validationResult.MemberNames.FirstOrDefault();
            if (fieldName != null)
            {
                var propertyName = $"{displayName}.{fieldName}";
                var errorMessage = validationResult.ErrorMessage.Replace(fieldName, propertyName);

                var memberNames = validationResult.MemberNames.Select(x => propertyName).ToList();
                var result = new ValidationResult(errorMessage, memberNames);

                this._validationResults.Add(result);
            }
        }

        public void Add(ValidationResult validationResult, string displayName, int index)
        {
            if (validationResult == null)
            {
                throw new ArgumentNullException(nameof(validationResult));
            }

            var fieldName = validationResult.MemberNames.FirstOrDefault();
            if (fieldName != null)
            {
                var propertyName = $"{displayName}[{index}].{fieldName}";
                var errorMessage = validationResult.ErrorMessage.Replace(fieldName, propertyName);

                var memberNames = validationResult.MemberNames.Select(x => propertyName).ToList();
                var result = new ValidationResult(errorMessage, memberNames);

                this._validationResults.Add(result);
            }
        }
    }
}