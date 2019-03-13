using System.Collections.Generic;
using System.Data;
using UnitTestProject2.ViewModel;

namespace UnitTestProject2.Repository
{
    public interface IEmployeeRepository
    {
        string ConnectionName { get; set; }

        object GetAll(out int count);

        IEnumerable<EmployeeViewModel> GetAllEmployees(out int count);

        IEnumerable<EmployeeViewModel> GetAllEmployeesDetail(out int count);
    }

    public interface IAdoEmployeeRepository
    {
        string ConnectionName { get; set; }

        DataTable GetAllEmployees(out int count);

        DataTable GetAllEmployeesDetail(out int count);
    }

}