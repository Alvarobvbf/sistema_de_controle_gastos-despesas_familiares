namespace OrcaLar.Api.Infrastructure;

/// <summary>
/// Exceção lançada pelos Services quando uma regra de negócio é violada (ex.: menor de idade
/// tentando cadastrar receita, ou pessoa inexistente referenciada por uma transação).
/// Capturada pelo ExceptionHandlingMiddleware, que a traduz em HTTP 422 + envelope de erro.
/// </summary>
public class RegraDeNegocioException : Exception
{
    /// <summary>Código curto e estável, usado pelo cliente da API para tratar o erro programaticamente.</summary>
    public string Code { get; }

    public RegraDeNegocioException(string code, string message) : base(message)
    {
        Code = code;
    }
}
