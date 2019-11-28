using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lab.DAL.UnitTest
{
    public static class DbOptionsFactory
    {
        public static DbContextOptions<LabEmployeeContext> DbContextOptions { get; }

        static DbOptionsFactory()
        {
            var configuration = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json")
                                .Build();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            DbContextOptions = new DbContextOptionsBuilder<LabEmployeeContext>()
                               .UseSqlServer(connectionString)
                               .Options;
        }
    }
}