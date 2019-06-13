using System;

namespace Lab.NoMagicNumeric.EntityModel.DAL
{
    public class Order
    {
        public Guid Id { get; set; }

        public string IsTransform { get; set; }

        public string Status { get; set; }
    }
}