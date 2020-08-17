using System;
using System.ComponentModel.DataAnnotations;
using Npoi.Mapper.Attributes;

namespace NETCore31
{
    internal class Employee
    {
        [Column("LocationID")]
        public string LocationId { get; set; }

        //[Column("DeptID")]
        [Display(Name = "DeptID")]
        public string DepartmentId { get; set; }

        [Column("DeptName")]
        public string DepartmentName { get; set; }

        [Column("EmployeeID")]

        public string EmployeeId { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("NTDomain")]
        public string DomainName { get; set; }

        [Column("ID")]
        public string Id { get; set; }

        public DateTime Birthdaty { get; set; }

        public string ErrorMessage { get; set; }
    }
}