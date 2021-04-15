using System;

namespace Lab.SQLite.DomainModel.Employee
{
    public class InsertOrderRequest
    {
        public Guid? Employee_Id { get; set; }

        public string Product_Id { get; set; }

        public string Product_Name { get; set; }

        public string Remark { get; set; }
    }
}