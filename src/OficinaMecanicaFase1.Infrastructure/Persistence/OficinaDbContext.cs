using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Domain.Cadastros;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class OficinaDbContext(DbContextOptions<OficinaDbContext> options) : DbContext(options)
{
    public DbSet<RegistroPersistencia> RegistrosPersistencia => Set<RegistroPersistencia>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegistroPersistencia>(entity =>
        {
            entity.ToTable("RegistrosPersistencia");
            entity.HasKey(registro => registro.Id);
            entity.Property(registro => registro.Descricao).HasMaxLength(200).IsRequired();
            entity.Property(registro => registro.CriadoEm).IsRequired();
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");
            entity.HasKey(cliente => cliente.Id);
            entity.Property(cliente => cliente.Nome).HasMaxLength(200).IsRequired();
            entity.Property(cliente => cliente.Documento).HasMaxLength(14).IsRequired();
            entity.HasIndex(cliente => cliente.Documento).IsUnique();
        });

        modelBuilder.Entity<Veiculo>(entity =>
        {
            entity.ToTable("Veiculos");
            entity.HasKey(veiculo => veiculo.Id);
            entity.Property(veiculo => veiculo.Placa).HasMaxLength(7).IsRequired();
            entity.Property(veiculo => veiculo.Marca).HasMaxLength(100).IsRequired();
            entity.Property(veiculo => veiculo.Modelo).HasMaxLength(100).IsRequired();
            entity.Property(veiculo => veiculo.Ano).IsRequired();
            entity.HasIndex(veiculo => veiculo.Placa).IsUnique();
            entity.HasIndex(veiculo => veiculo.ClienteId);
            entity.HasOne<Cliente>().WithMany().HasForeignKey(veiculo => veiculo.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
