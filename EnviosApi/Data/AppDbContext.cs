using EnviosApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviosApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Envio> Envios => Set<Envio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>()
            .HasIndex(c => c.Dni).IsUnique();

        modelBuilder.Entity<Envio>()
            .Property(e => e.Estado).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<Envio>()
            .HasOne(e => e.Cliente)
            .WithMany(c => c.Envios)
            .HasForeignKey(e => e.ClienteId);
    }
}