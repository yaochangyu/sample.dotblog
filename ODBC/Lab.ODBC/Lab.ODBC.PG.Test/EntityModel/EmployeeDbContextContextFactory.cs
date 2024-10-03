using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lab.ODBC.PG.Test.EntityModel;

public class EmployeeDbContextContextFactory : IDesignTimeDbContextFactory<EmployeeDbContext>
{
    public EmployeeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EmployeeDbContext>();
        optionsBuilder
            .UseNpgsql("Host=localhost;Port=5432;Database=employee;Username=postgres;Password=guest")
            ;

        return new EmployeeDbContext(optionsBuilder.Options);
    }
}