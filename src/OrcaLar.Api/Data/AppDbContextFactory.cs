using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrcaLar.Api.Data;

/// <summary>
/// Fábrica usada apenas em tempo de design (comandos "dotnet ef migrations ...") para criar
/// o AppDbContext sem depender da variável de ambiente DATABASE_URL exigida pelo Program.cs
/// em runtime. A connection string aqui é só um placeholder para o EF inspecionar o modelo
/// e gerar as migrations — nunca é usada para conectar de fato em produção.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=orcalar;Username=orcalar;Password=orcalar");
        return new AppDbContext(optionsBuilder.Options);
    }
}
