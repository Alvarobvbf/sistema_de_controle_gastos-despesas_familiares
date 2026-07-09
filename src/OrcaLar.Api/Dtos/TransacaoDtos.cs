using System.ComponentModel.DataAnnotations;
using OrcaLar.Api.Domain.Entities;

namespace OrcaLar.Api.Dtos;

/// <summary>
/// Corpo esperado por POST /api/transacoes.
/// Tipo é declarado como TipoTransacao? (em vez do enum puro) para que a ausência do campo
/// seja pega pelo [Required] — com o enum puro, um campo omitido cairia silenciosamente no
/// primeiro valor (Despesa), mascarando o erro de payload incompleto.
/// </summary>
public record CriarTransacaoRequest(
    [Required(AllowEmptyStrings = false, ErrorMessage = "A descrição é obrigatória.")]
    string Descricao,
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    decimal Valor,
    [Required(ErrorMessage = "O tipo é obrigatório.")]
    TipoTransacao? Tipo,
    Guid PessoaId
);

/// <summary>Representação de Transacao devolvida pela API, já com o nome da pessoa embutido.</summary>
public record TransacaoDto(
    Guid Id,
    string Descricao,
    decimal Valor,
    TipoTransacao Tipo,
    Guid PessoaId,
    string PessoaNome
);
