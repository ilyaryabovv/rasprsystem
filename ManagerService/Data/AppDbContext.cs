using ManagerService.Models;
using Microsoft.EntityFrameworkCore;

namespace ManagerService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Manager> Managers => Set<Manager>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Manager>(entity =>
        {
            entity.ToTable("managers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(255).IsRequired();
            entity.Property(x => x.ContractsCount).HasColumnName("contracts_count").IsRequired();
        });
    }
}
