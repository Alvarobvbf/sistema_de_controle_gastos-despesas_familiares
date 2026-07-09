using System.ComponentModel.DataAnnotations;

namespace OrcaLar.Api.Dtos;

/// <summary>
/// Corpo esperado por POST /api/pessoas.
/// Idade é declarada como int? (em vez de int) para que a ausência do campo no JSON seja
/// detectável pelo [Required] — com int puro, um campo omitido cairia silenciosamente em 0,
/// que é um valor válido de idade, mascarando o erro de payload incompleto.
/// </summary>
public record CriarPessoaRequest(
    [Required(AllowEmptyStrings = false, ErrorMessage = "O nome é obrigatório.")]
    string Nome,
    [Required(ErrorMessage = "A idade é obrigatória.")]
    [Range(0, 150, ErrorMessage = "A idade deve estar entre 0 e 150.")]
    int? Idade
);

/// <summary>Representação de Pessoa devolvida pela API.</summary>
public record PessoaDto(Guid Id, string Nome, int Idade);
