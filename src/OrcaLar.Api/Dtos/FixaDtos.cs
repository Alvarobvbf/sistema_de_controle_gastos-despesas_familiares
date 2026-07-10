using System.ComponentModel.DataAnnotations;
using OrcaLar.Api.Domain.Entities;

namespace OrcaLar.Api.Dtos;

/// <summary>
/// Corpo esperado por POST /api/fixas. DataInicio é opcional (o FixaService preenche com a
/// data atual do servidor quando omitida, mesmo critério usado em Transacao).
/// </summary>
public record CriarFixaRequest(
    [Required(AllowEmptyStrings = false, ErrorMessage = "A descrição é obrigatória.")]
    string Descricao,
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    decimal Valor,
    [Required(ErrorMessage = "O tipo é obrigatório.")]
    TipoTransacao? Tipo,
    Guid PessoaId,
    [Range(1, 31, ErrorMessage = "O dia do mês deve estar entre 1 e 31.")]
    int DiaDoMes,
    DateOnly? DataInicio,
    DateOnly? DataFim
) : IValidatableObject
{
    // Validação cruzada de formato (não depende de estado do banco, por isso vive aqui como
    // IValidatableObject e não no Service) — só se aplica quando ambas as datas vêm no payload;
    // quando DataInicio é omitida, o Service resolve para "hoje" e essa checagem não se aplica.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataInicio is not null && DataFim is not null && DataFim < DataInicio)
        {
            yield return new ValidationResult(
                "A data de fim deve ser maior ou igual à data de início.",
                [nameof(DataFim)]);
        }
    }
}

/// <summary>Representação de Fixa devolvida pela API, já com o nome da pessoa embutido.</summary>
public record FixaDto(
    Guid Id,
    string Descricao,
    decimal Valor,
    TipoTransacao Tipo,
    Guid PessoaId,
    string PessoaNome,
    int DiaDoMes,
    DateOnly DataInicio,
    DateOnly? DataFim
);
