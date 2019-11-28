using Lab.DAL.EntityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lab.DAL
{
    public class DbOptionsFactory
    {
        public static DbContextOptions<LabEmployeeContext> DbContextOptions { get; }

        public static string ConnectionString { get; }

        static DbOptionsFactory()
        {
            var configuration = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json")
                                .Build();
            ConnectionString = configuration.GetConnectionString("DefaultConnection");

            DbContextOptions = new DbContextOptionsBuilder<LabEmployeeContext>()
                               .UseSqlServer(ConnectionString)
                               .Options;
        }
    }
}