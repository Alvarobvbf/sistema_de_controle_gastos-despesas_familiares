using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;

namespace OrcaLar.Api.Infrastructure;

/// <summary>
/// Aplica as migrations pendentes do EF Core na subida da API.
/// </summary>
public static class MigrationRunner
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan DelayBetweenAttempts = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Tenta migrar o banco com retry: no primeiro boot (em especial via docker compose) o
    /// container do Postgres pode ainda não estar pronto para aceitar conexões quando a API
    /// sobe, e sem essa espera o primeiro start quebraria numa corrida entre os dois serviços.
    /// Na última tentativa, a exceção é deixada propagar para falhar o boot de forma visível.
    /// </summary>
    public static async Task MigrateWithRetryAsync(this IServiceProvider services, ILogger logger)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Falha ao aplicar migrations (tentativa {Attempt}/{MaxAttempts}). Tentando novamente em {Delay}s...",
                    attempt,
                    MaxAttempts,
                    DelayBetweenAttempts.TotalSeconds);
                await Task.Delay(DelayBetweenAttempts);
            }
        }
    }
}
