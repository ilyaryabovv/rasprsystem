using ManagerService.Models;
using Microsoft.EntityFrameworkCore;

namespace ManagerService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<ExportSaga> ExportSagas => Set<ExportSaga>();

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

        modelBuilder.Entity<ExportSaga>(entity =>
        {
            entity.ToTable("export_saga");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
            entity.Property(x => x.Instance).HasColumnName("instance").HasMaxLength(120);
        });
    }
}
