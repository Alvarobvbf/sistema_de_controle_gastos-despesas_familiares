using Npgsql;

namespace OrcaLar.Api.Infrastructure;

/// <summary>
/// Converte a variável de ambiente DATABASE_URL (formato URI padrão do Postgres, o mesmo
/// que o Railway injeta: postgresql://usuario:senha@host:porta/banco) em uma connection
/// string entendida pelo Npgsql.
/// </summary>
public static class DatabaseUrlParser
{
    /// <summary>
    /// Monta a connection string a partir da DATABASE_URL, decidindo o modo de SSL conforme
    /// o host de destino:
    /// - Railway expõe hosts com domínio real (ex.: algo.proxy.rlwy.net ou algo.railway.internal)
    ///   e exige SSL para conectar.
    /// - Localmente (localhost/127.0.0.1 rodando na máquina do dev, ou um nome de serviço do
    ///   docker compose, como "db", que nunca contém ponto) o Postgres não expõe SSL.
    /// Por isso o host é usado como sinal: presença de ponto no nome (e não ser loopback)
    /// indica um domínio "de verdade" (Railway) e liga SSL Require + TrustServerCertificate;
    /// caso contrário, SSL é desligado.
    /// </summary>
    public static string ToNpgsqlConnectionString(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new InvalidOperationException(
                "A variável de ambiente DATABASE_URL não foi definida. Configure-a com a " +
                "string de conexão do Postgres no formato postgresql://usuario:senha@host:porta/banco " +
                "antes de iniciar a API (veja o .env.example).");
        }

        Uri uri;
        try
        {
            uri = new Uri(databaseUrl);
        }
        catch (UriFormatException ex)
        {
            throw new InvalidOperationException(
                "A variável de ambiente DATABASE_URL está em um formato inválido. Esperado: " +
                "postgresql://usuario:senha@host:porta/banco.", ex);
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = database,
            Username = username,
            Password = password
        };

        if (IsLocalHost(uri.Host))
        {
            connectionStringBuilder.SslMode = SslMode.Disable;
        }
        else
        {
            // No Npgsql 8+, SslMode.Require já não valida o certificado do servidor (esse é
            // o comportamento que antes exigia TrustServerCertificate=true, hoje obsoleto) —
            // suficiente para conectar em hosts com certificado autoassinado, como o Railway.
            connectionStringBuilder.SslMode = SslMode.Require;
        }

        return connectionStringBuilder.ConnectionString;
    }

    private static bool IsLocalHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1"
            || host == "::1"
            || !host.Contains('.');
    }
}
