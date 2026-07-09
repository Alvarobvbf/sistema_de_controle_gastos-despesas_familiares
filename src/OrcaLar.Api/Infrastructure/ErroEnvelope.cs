namespace OrcaLar.Api.Infrastructure;

/// <summary>
/// Formato único de erro devolvido pela API, tanto para 400 (validação de formato) quanto
/// para 422 (violação de regra de negócio): { "error": { "code": "...", "message": "..." } }.
/// </summary>
public record ErroDetalhe(string Code, string Message);

public record ErroEnvelope(ErroDetalhe Error);
