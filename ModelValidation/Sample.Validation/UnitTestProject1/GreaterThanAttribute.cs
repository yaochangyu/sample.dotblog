using System;
using System.ComponentModel.DataAnnotations;

namespace UnitTestProject1
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class GreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonPropertyName;

        public GreaterThanAttribute(string comparisonPropertyName)
        {
            this._comparisonPropertyName = comparisonPropertyName;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            this.ErrorMessage = this.ErrorMessageString;
            if (value.GetType() == typeof(IComparable))
            {
                throw new ArgumentException("value has not implemented IComparable interface");
            }

            var currentValue = (IComparable) value;
            var comparisonPropertyInfo= validationContext.ObjectType.GetProperty(this._comparisonPropertyName);
            if (comparisonPropertyInfo == null)
            {
                throw new ArgumentException("Comparison property with this name not found");
            }

            var comparisonValue = comparisonPropertyInfo.GetValue(validationContext.ObjectInstance);
            if (comparisonValue.GetType() == typeof(IComparable))
            {
                throw new ArgumentException("Comparison property has not implemented IComparable interface");
            }

            if (!ReferenceEquals(value.GetType(), comparisonValue.GetType()))
            {
                throw new ArgumentException("The properties types must be the same");
            }

            if (currentValue.CompareTo((IComparable) comparisonValue) < 0)
            {
                return new ValidationResult(this.ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}