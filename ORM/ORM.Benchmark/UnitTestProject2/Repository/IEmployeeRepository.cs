using System.Collections.Generic;
using System.Data;
using LinqToDB.Common;
using UnitTestProject2.ViewModel;

namespace UnitTestProject2.Repository
{
    public interface IEmployeeRepository
    {
        string ConnectionName { get; set; }

        //IEnumerable<EmployeeViewModel> GetAllEmployees(out int count);
        //IEnumerable<T> GetAllEmployees<T>(out int count);
        IEnumerable<EmployeeViewModel> GetAllEmployees(out int count);
        IEnumerable<EmployeeViewModel> GetAllEmployeesDetail(out int count);
    }
    public interface IAdoEmployeeRepository
    {
        string ConnectionName { get; set; }

        //IEnumerable<EmployeeViewModel> GetAllEmployees(out int count);
        //IEnumerable<T> GetAllEmployees<T>(out int count);
        DataTable GetAllEmployees(out int count);
    }
}