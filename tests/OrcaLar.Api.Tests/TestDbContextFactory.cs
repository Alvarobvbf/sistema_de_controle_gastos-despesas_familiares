using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;

namespace OrcaLar.Api.Tests;

/// <summary>
/// Cria um AppDbContext com o provider EF Core InMemory, isolado por teste (banco com nome
/// único), evitando que um teste veja dados criados por outro.
/// </summary>
internal static class TestDbContextFactory
{
    public static AppDbContext Criar()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
