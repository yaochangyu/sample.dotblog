using Lab.DeviceFingerprint.WebApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lab.DeviceFingerprint.WebApi.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.FingerprintHash }).IsUnique();
            entity.Property(e => e.FingerprintHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.DeviceName).HasMaxLength(200);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Devices)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
