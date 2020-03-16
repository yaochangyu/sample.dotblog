using System;
using Lab.EntityModel;
using LinqToDB;

namespace Lab.UnitTest
{

    internal class EmployeeRepository2 : IDisposable
    {
        private readonly LabEmployee2DB _db;

        private bool disposed;

        public EmployeeRepository2(string connectionStringName)
        {
            this._db = new LabEmployee2DB(connectionStringName);
        }

        public int Insert(Employee employee)
        {
            var result = 0;

            //TODO:寫你的操作
            result = this._db.Insert(new Employee {Id = Guid.NewGuid(), Name = "小章", Age = 18});

            return result;
        }

        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    this._db?.Dispose();
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                // Note disposing has been done.
                this.disposed = true;
            }
        }

        ~EmployeeRepository2()
        {
            this.Dispose();
        }
    }
}