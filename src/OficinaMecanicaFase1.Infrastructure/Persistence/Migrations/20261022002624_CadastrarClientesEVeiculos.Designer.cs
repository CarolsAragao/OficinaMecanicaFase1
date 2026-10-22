using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OficinaMecanicaFase1.Infrastructure.Persistence;

#nullable disable

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(OficinaDbContext))]
    [Migration("20261022002624_CadastrarClientesEVeiculos")]
    partial class CadastrarClientesEVeiculos
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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
#pragma warning restore 612, 618
        }
    }
}
