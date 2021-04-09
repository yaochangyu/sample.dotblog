using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;

namespace Lab.DAL
{
    public class EmployeeContextFactory
    {
        private DbContextOptions<EmployeeContext> _options;

        public EmployeeContextFactory(DbContextOptions<EmployeeContext> options)
        {
            this._options = options;
        }

        public EmployeeContextFactory():this(DbContextOptionManager.CreateEmployeeDbContextOptions())
        {
        }
    }
}