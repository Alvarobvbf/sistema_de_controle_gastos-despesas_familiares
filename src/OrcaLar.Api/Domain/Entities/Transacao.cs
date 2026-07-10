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

    /// <summary>
    /// Data em que a transação ocorreu. Opcional na criação (o Service preenche com a data
    /// atual do servidor quando omitida) — ver TransacaoService.CriarAsync.
    /// </summary>
    public DateOnly Data { get; set; }

    public Guid PessoaId { get; set; }

    public Pessoa Pessoa { get; set; } = null!;
}
