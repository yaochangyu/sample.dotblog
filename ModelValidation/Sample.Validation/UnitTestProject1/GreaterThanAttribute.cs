using System;
using System.ComponentModel.DataAnnotations;

namespace UnitTestProject1
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class GreaterThanAttribute : ValidationAttribute
    {
        private readonly string _targetName;

        public GreaterThanAttribute(string targetFieldName)
        {
            this._targetName = targetFieldName;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            this.ErrorMessage = this.ErrorMessageString;
            var sourceType = value.GetType();
            var sourceName = validationContext.MemberName;

            if (sourceType == typeof(IComparable))
            {
                throw new ArgumentException("value has not implemented IComparable interface");
            }

            var sourceValue = (IComparable) value;
            var comparisonPropertyInfo = validationContext.ObjectType.GetProperty(this._targetName);
            if (comparisonPropertyInfo == null)
            {
                throw new ArgumentException("Comparison property with this name not found");
            }

            var targetValue = comparisonPropertyInfo.GetValue(validationContext.ObjectInstance);
            var targetType = targetValue.GetType();
            if (targetType == typeof(IComparable))
            {
                throw new ArgumentException("Comparison property has not implemented IComparable interface");
            }

            if (!ReferenceEquals(sourceType, targetType))
            {
                throw new ArgumentException("The properties types must be the same");
            }

            if (sourceValue.CompareTo((IComparable) targetValue) < 0)
            {
                this.ErrorMessage = $"{this._targetName} property must be less than the {sourceName} property";
                return new ValidationResult(this.ErrorMessage, new[] {this._targetName, sourceName});
            }

            return ValidationResult.Success;
        }
    } 
}