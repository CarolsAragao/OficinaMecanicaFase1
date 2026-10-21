using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace OficinaMecanicaFase1.Infrastructure.Identity;

public sealed class IdentidadeDbContext(DbContextOptions<IdentidadeDbContext> options)
    : IdentityDbContext<UsuarioInterno, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<UsuarioInterno>()
            .HasIndex(usuario => usuario.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("EmailIndex")
            .HasFilter("[NormalizedEmail] IS NOT NULL");
    }
}
