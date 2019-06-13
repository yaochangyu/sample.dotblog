using System;

namespace Lab.NoMagicNumeric.DAL
{
    public class DefineAttribute : Attribute
    {
        public string Code { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }

        public int Value { get; set; }

        public DefineAttribute(string code, string description)
        {
            this.Code        = code;
            this.Description = description;
        }
    }
}