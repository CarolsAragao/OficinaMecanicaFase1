using Microsoft.EntityFrameworkCore;
using OficinaMecanicaFase1.Domain.Cadastros;
using OficinaMecanicaFase1.Domain.Catalogo;
using OficinaMecanicaFase1.Domain.Estoque;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class OficinaDbContext(DbContextOptions<OficinaDbContext> options) : DbContext(options)
{
    public DbSet<RegistroPersistencia> RegistrosPersistencia => Set<RegistroPersistencia>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<ServicoCatalogo> ServicosCatalogo => Set<ServicoCatalogo>();
    public DbSet<ItemEstoque> ItensEstoque => Set<ItemEstoque>();
    public DbSet<EntradaEstoque> EntradasEstoque => Set<EntradaEstoque>();

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

        modelBuilder.Entity<ServicoCatalogo>(entity =>
        {
            entity.ToTable("ServicosCatalogo", table =>
            {
                table.HasCheckConstraint("CK_ServicosCatalogo_PrecoBase", "[PrecoBase] >= 0");
            });
            entity.HasKey(servico => servico.Id);
            entity.Property(servico => servico.Nome).HasMaxLength(200).IsRequired();
            entity.Property(servico => servico.Descricao).HasMaxLength(500);
            entity.Property(servico => servico.PrecoBase).HasPrecision(18, 2).IsRequired();
            entity.Property(servico => servico.Ativo).IsRequired();
            entity.HasIndex(servico => new { servico.Ativo, servico.Nome });
        });

        modelBuilder.Entity<ItemEstoque>(entity =>
        {
            entity.ToTable("ItensEstoque", table =>
            {
                table.HasCheckConstraint("CK_ItensEstoque_Tipo", "[Tipo] IN (1, 2)");
                table.HasCheckConstraint("CK_ItensEstoque_PrecoUnitario", "[PrecoUnitario] >= 0");
                table.HasCheckConstraint("CK_ItensEstoque_Saldos", "[SaldoFisico] >= 0 AND [SaldoReservado] >= 0 AND [SaldoReservado] <= [SaldoFisico]");
                table.HasCheckConstraint("CK_ItensEstoque_PecaInteira", "[Tipo] <> 1 OR ([SaldoFisico] = FLOOR([SaldoFisico]) AND [SaldoReservado] = FLOOR([SaldoReservado]))");
            });
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Nome).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Descricao).HasMaxLength(500);
            entity.Property(item => item.Tipo).HasConversion<int>().IsRequired();
            entity.Property(item => item.UnidadeMedida).HasMaxLength(20).IsRequired();
            entity.Property(item => item.PrecoUnitario).HasPrecision(18, 2).IsRequired();
            entity.Property(item => item.SaldoFisico).HasPrecision(18, 3).IsRequired();
            entity.Property(item => item.SaldoReservado).HasPrecision(18, 3).IsRequired();
            entity.Property(item => item.Versao).IsRowVersion();
            entity.Ignore(item => item.SaldoDisponivel);
            entity.HasIndex(item => new { item.Ativo, item.Nome });
        });

        modelBuilder.Entity<EntradaEstoque>(entity =>
        {
            entity.ToTable("EntradasEstoque", table =>
                table.HasCheckConstraint("CK_EntradasEstoque_Quantidade", "[Quantidade] > 0"));
            entity.HasKey(entrada => entrada.Id);
            entity.Property(entrada => entrada.Quantidade).HasPrecision(18, 3).IsRequired();
            entity.Property(entrada => entrada.RegistradaEmUtc).IsRequired();
            entity.Property(entrada => entrada.UsuarioId).IsRequired();
            entity.HasIndex(entrada => new { entrada.ItemEstoqueId, entrada.RegistradaEmUtc });
            entity.HasOne<ItemEstoque>().WithMany().HasForeignKey(entrada => entrada.ItemEstoqueId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
