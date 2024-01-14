using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lab.Snapshot.DB;

public class MemberDbContextFactory : IDesignTimeDbContextFactory<MemberDbContext>
{
    public MemberDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MemberDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=member;Username=postgres;Password=guest");

        return new MemberDbContext(optionsBuilder.Options);
    }
}