namespace OrcaLar.Api.Domain.Entities;

/// <summary>
/// Representa um lançamento financeiro (receita ou despesa) associado a uma Pessoa.
/// Não possui edição nem deleção conforme o enunciado — apenas criação e listagem.
/// </summary>
public class Transacao
{
    public Guid Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    /// <summary>
    /// Valor sempre positivo: o sinal/significado (entrada ou saída) vem exclusivamente
    /// do campo Tipo, nunca do sinal do próprio Valor.
    /// </summary>
    public decimal Valor { get; set; }

    public TipoTransacao Tipo { get; set; }

    public Guid PessoaId { get; set; }

    public Pessoa Pessoa { get; set; } = null!;
}
