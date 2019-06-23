using System;

namespace Lab.ExceptionStack.BLL
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property,AllowMultiple = false)]
    public class ErrorDescriptionAttribute : Attribute
    {
        public string Description { get; set; }

        public ErrorDescriptionAttribute(string description)
        {
            this.Description = description;
        }
    }
}