using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OficinaMecanicaFase1.Infrastructure.Persistence;

public sealed class OficinaDbContextFactory : IDesignTimeDbContextFactory<OficinaDbContext>
{
    public OficinaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OficinaDbContext>()
            .UseSqlServer("Server=localhost;Database=OficinaMecanicaDesign;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new OficinaDbContext(options);
    }
}
