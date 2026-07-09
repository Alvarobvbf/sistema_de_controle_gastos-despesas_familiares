namespace OrcaLar.Api.Infrastructure;

/// <summary>
/// Códigos curtos e estáveis usados no envelope de erro ({ "error": { "code", "message" } }),
/// centralizados aqui para não haver strings mágicas espalhadas pelos Services.
/// </summary>
public static class CodigosErro
{
    /// <summary>400 — payload malformado ou falha de validação de formato (DataAnnotations).</summary>
    public const string DadosInvalidos = "DADOS_INVALIDOS";

    /// <summary>422 — tentativa de cadastrar Receita para pessoa com idade menor que 18.</summary>
    public const string RegraMenorReceita = "REGRA_MENOR_RECEITA";

    /// <summary>422 — pessoaId informado em uma transação não existe no cadastro.</summary>
    public const string PessoaNaoEncontrada = "PESSOA_NAO_ENCONTRADA";
}
