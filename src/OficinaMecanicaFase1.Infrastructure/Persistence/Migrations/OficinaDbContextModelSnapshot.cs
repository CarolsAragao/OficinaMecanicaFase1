using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OficinaMecanicaFase1.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OficinaDbContext))]
public sealed class OficinaDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);
        modelBuilder.UseIdentityColumns();

        modelBuilder.Entity("OficinaMecanicaFase1.Infrastructure.Persistence.RegistroPersistencia", entity =>
        {
            entity.Property<Guid>("Id").HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("CriadoEm").HasColumnType("datetimeoffset");
            entity.Property<string>("Descricao")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");
            entity.HasKey("Id");
            entity.ToTable("RegistrosPersistencia");
        });
    }
}
