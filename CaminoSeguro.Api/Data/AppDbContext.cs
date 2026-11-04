using CaminoSeguro.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CaminoSeguro.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Municipio> Municipios => Set<Municipio>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Reporte> Reportes => Set<Reporte>();
    public DbSet<Confirmacion> Confirmaciones => Set<Confirmacion>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Usuario>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.Property(x => x.Alias).HasMaxLength(30).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Municipio>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(80).IsRequired();
        });

        b.Entity<Categoria>(e =>
        {
            e.Property(x => x.Nombre).HasMaxLength(60).IsRequired();
            e.Property(x => x.ColorHex).HasMaxLength(7).IsRequired();
        });

        b.Entity<Reporte>(e =>
        {
            e.Property(x => x.PuntoReferencia).HasMaxLength(200).IsRequired();
            e.Property(x => x.Descripcion).HasMaxLength(500);
            e.Property(x => x.FranjaHoraria).HasMaxLength(20);
            e.Property(x => x.FuenteCoordenadas).HasMaxLength(20).IsRequired();

            e.HasOne(x => x.Usuario).WithMany(u => u.Reportes)
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Municipio).WithMany()
                .HasForeignKey(x => x.MunicipioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Categoria).WithMany()
                .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.MunicipioId);
            e.HasIndex(x => x.CategoriaId);
            e.HasIndex(x => x.FechaCreacion);
        });

        b.Entity<Confirmacion>(e =>
        {
            // Una usuaria solo puede confirmar una vez cada reporte.
            e.HasKey(x => new { x.ReporteId, x.UsuarioId });
            e.HasOne(x => x.Reporte).WithMany(r => r.Confirmaciones)
                .HasForeignKey(x => x.ReporteId).OnDelete(DeleteBehavior.Cascade);
            // Restrict evita "multiple cascade paths" en SQL Server.
            e.HasOne(x => x.Usuario).WithMany()
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });

        Semilla.Cargar(b);
    }
}
