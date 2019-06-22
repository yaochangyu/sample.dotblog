using System;

namespace Lab.ExceptionStack.BLL
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property,AllowMultiple = false)]
    public class ErrorDescription : Attribute
    {
        public string Description { get; set; }

        public ErrorDescription(string description)
        {
            this.Description = description;
        }
    }
}