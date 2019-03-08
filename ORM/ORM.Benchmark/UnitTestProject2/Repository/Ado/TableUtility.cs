using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject2.Repository.Ado
{
    class TableUtility
    {
        public static DataTable s_employeeTable;
        public static DataTable GetEmployeeTable()
        {
            if (s_employeeTable == null)
            {
                s_employeeTable = new DataTable();
                s_employeeTable.Columns.Add(new DataColumn("Id", typeof(Guid)));
                s_employeeTable.Columns.Add(new DataColumn("Name", typeof(string)));
                s_employeeTable.Columns.Add(new DataColumn("Age", typeof(int)));
                s_employeeTable.Columns.Add(new DataColumn("Remark", typeof(string)));
            }
            s_employeeTable.Clear();
            return s_employeeTable;
        }
    }
}
