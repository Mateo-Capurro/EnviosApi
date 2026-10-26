using EnviosApi.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviosApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Envio> Envios => Set<Envio>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

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
        modelBuilder.Entity<Cliente>().Property(c => c.Nombre).HasMaxLength(100);
        modelBuilder.Entity<Cliente>().Property(c => c.Email).HasMaxLength(150);
        modelBuilder.Entity<Envio>().Property(e => e.Direccion).HasMaxLength(200);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Usuario>().Property(u => u.Email).HasMaxLength(150);
        modelBuilder.Entity<Usuario>().Property(u => u.PasswordHash).HasMaxLength(100);
        modelBuilder.Entity<Usuario>().Property(u => u.Rol).HasMaxLength(20);

    }
}