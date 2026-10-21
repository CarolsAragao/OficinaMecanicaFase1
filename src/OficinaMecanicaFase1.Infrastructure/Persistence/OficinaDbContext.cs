using Microsoft.EntityFrameworkCore;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class OficinaDbContext(DbContextOptions<OficinaDbContext> options) : DbContext(options)
{
    public DbSet<RegistroPersistencia> RegistrosPersistencia => Set<RegistroPersistencia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistroPersistencia>(entity =>
        {
            entity.ToTable("RegistrosPersistencia");
            entity.HasKey(registro => registro.Id);
            entity.Property(registro => registro.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(registro => registro.CriadoEm).IsRequired();
        });
    }
}
