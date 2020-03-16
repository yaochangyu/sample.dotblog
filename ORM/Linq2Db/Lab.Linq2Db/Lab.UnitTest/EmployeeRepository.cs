using System;
using System.Data.Common;
using Lab.EntityModel;
using LinqToDB;

namespace Lab.UnitTest
{
    internal class EmployeeRepository
    {
        public string ConnectionName { get; set; } = "LabDbContext";

        public int Insert(Employee employee, DbConnection dbConnection = null)
        {
            var            result    = 0;
            var            isDispose = false;
            LabEmployee2DB dbContext = null;

            try
            {
                if (dbConnection == null)
                {
                    dbContext = new LabEmployee2DB(this.ConnectionName);
                    isDispose = true;
                }

                //TODO:寫你的操作
                result = dbContext.Insert(new Employee {Id = Guid.NewGuid(), Name = "小章", Age = 18});
            }
            finally
            {
                if (isDispose)
                {
                    if (dbContext != null)
                    {
                        dbContext.Dispose();
                    }
                }
            }

            return result;
        }
    }
}