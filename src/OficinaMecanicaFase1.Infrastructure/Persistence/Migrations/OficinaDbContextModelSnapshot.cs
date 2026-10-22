using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OficinaMecanicaFase1.Infrastructure.Persistence;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(OficinaDbContext))]
    partial class OficinaDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.13")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("OficinaMecanicaFase1.Domain.Cadastros.Cliente", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Documento")
                        .IsRequired()
                        .HasMaxLength(14)
                        .HasColumnType("nvarchar(14)");

                    b.Property<string>("Nome")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    b.HasIndex("Documento")
                        .IsUnique();

                    b.ToTable("Clientes", (string)null);
                });

            modelBuilder.Entity("OficinaMecanicaFase1.Domain.Cadastros.Veiculo", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Ano")
                        .HasColumnType("int");

                    b.Property<Guid>("ClienteId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<string>("Marca")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Modelo")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Placa")
                        .IsRequired()
                        .HasMaxLength(7)
                        .HasColumnType("nvarchar(7)");

                    b.HasKey("Id");

                    b.HasIndex("ClienteId");

                    b.HasIndex("Placa")
                        .IsUnique();

                    b.ToTable("Veiculos", (string)null);
                });

            modelBuilder.Entity("OficinaMecanicaFase1.Domain.Catalogo.ServicoCatalogo", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("Ativo")
                        .HasColumnType("bit");

                    b.Property<string>("Descricao")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Nome")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<decimal>("PrecoBase")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("Ativo", "Nome");

                    b.ToTable("ServicosCatalogo", null, t =>
                        {
                            t.HasCheckConstraint("CK_ServicosCatalogo_PrecoBase", "[PrecoBase] >= 0");
                        });
                });

            modelBuilder.Entity("OficinaMecanicaFase1.Domain.Estoque.EntradaEstoque", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("ItemEstoqueId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<decimal>("Quantidade")
                        .HasPrecision(18, 3)
                        .HasColumnType("decimal(18,3)");

                    b.Property<DateTimeOffset>("RegistradaEmUtc")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("UsuarioId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("ItemEstoqueId", "RegistradaEmUtc");

                    b.ToTable("EntradasEstoque", null, t =>
                        {
                            t.HasCheckConstraint("CK_EntradasEstoque_Quantidade", "[Quantidade] > 0");
                        });
                });

            modelBuilder.Entity("OficinaMecanicaFase1.Domain.Estoque.ItemEstoque", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("Ativo")
                        .HasColumnType("bit");

                    b.Property<string>("Descricao")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Nome")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<decimal>("PrecoUnitario")
                        .HasPrecision(18, 2)
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("SaldoFisico")
                        .HasPrecision(18, 3)
                        .HasColumnType("decimal(18,3)");

                    b.Property<decimal>("SaldoReservado")
                        .HasPrecision(18, 3)
                        .HasColumnType("decimal(18,3)");

                    b.Property<int>("Tipo")
                        .HasColumnType("int");

                    b.Property<string>("UnidadeMedida")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<byte[]>("Versao")
                        .IsConcurrencyToken()
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.HasKey("Id");

                    b.HasIndex("Ativo", "Nome");

                    b.ToTable("ItensEstoque", null, t =>
                        {
                            t.HasCheckConstraint("CK_ItensEstoque_PecaInteira", "[Tipo] <> 1 OR ([SaldoFisico] = FLOOR([SaldoFisico]) AND [SaldoReservado] = FLOOR([SaldoReservado]))");

                            t.HasCheckConstraint("CK_ItensEstoque_PrecoUnitario", "[PrecoUnitario] >= 0");

                            t.HasCheckConstraint("CK_ItensEstoque_Saldos", "[SaldoFisico] >= 0 AND [SaldoReservado] >= 0 AND [SaldoReservado] <= [SaldoFisico]");

                            t.HasCheckConstraint("CK_ItensEstoque_Tipo", "[Tipo] IN (1, 2)");
                        });
                });

            modelBuilder.Entity("OficinaMecanicaFase1.Infrastructure.Persistence.RegistroPersistencia", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("CriadoEm")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Descricao")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.HasKey("Id");

                    b.ToTable("RegistrosPersistencia", (string)null);
                });

            modelBuilder.Entity("OficinaMecanicaFase1.Domain.Cadastros.Veiculo", b =>
                {
                    b.HasOne("OficinaMecanicaFase1.Domain.Cadastros.Cliente", null)
                        .WithMany()
                        .HasForeignKey("ClienteId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });

            modelBuilder.Entity("OficinaMecanicaFase1.Domain.Estoque.EntradaEstoque", b =>
                {
                    b.HasOne("OficinaMecanicaFase1.Domain.Estoque.ItemEstoque", null)
                        .WithMany()
                        .HasForeignKey("ItemEstoqueId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
