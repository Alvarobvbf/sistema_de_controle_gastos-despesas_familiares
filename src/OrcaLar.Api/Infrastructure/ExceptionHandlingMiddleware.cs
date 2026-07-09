using System.Text.Json;

namespace OrcaLar.Api.Infrastructure;

/// <summary>
/// Middleware global que traduz RegraDeNegocioException (lançada pelos Services) em uma
/// resposta HTTP 422 com o envelope de erro padrão da API. Mantém os Controllers livres de
/// try/catch — eles só orquestram, a regra e o tratamento de violação ficam nos Services
/// e aqui, respectivamente.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (RegraDeNegocioException ex)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            context.Response.ContentType = "application/json";

            var envelope = new ErroEnvelope(new ErroDetalhe(ex.Code, ex.Message));
            await context.Response.WriteAsync(JsonSerializer.Serialize(envelope, JsonOptions));
        }
    }
}
