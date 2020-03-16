using System;
using Lab.EntityModel;
using LinqToDB;

namespace Lab.UnitTest
{
    internal class EmployeeRepository1
    {
        public string ConnectionName { get; set; } = "LabDbContext";

        public int Insert(Employee employee, LabEmployee2DB db)
        {
            var result    = 0;
            var isDispose = false;
            try
            {
                if (db == null)
                {
                    db        = new LabEmployee2DB(this.ConnectionName);
                    isDispose = true;
                }

                //TODO:寫你的操作
                result = db.Insert(new Employee {Id = Guid.NewGuid(), Name = "小章", Age = 18});
            }
            finally
            {
                if (isDispose)
                {
                    if (db != null)
                    {
                        db.Dispose();
                    }
                }
            }

            return result;
        }
    }
}