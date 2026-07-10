namespace OrcaLar.Api.Domain.Entities;

/// <summary>
/// Representa uma REGRA de lançamento recorrente (ex.: aluguel, mesada) — não é uma
/// transação e nunca gera linhas em Transacao. É expandida em ocorrências virtuais sob
/// demanda pelo motor de projeção (ver Services/MotorProjecaoFixas.cs), usada apenas para
/// a série projetada do dashboard.
/// </summary>
public class Fixa
{
    public Guid Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public TipoTransacao Tipo { get; set; }

    public Guid PessoaId { get; set; }

    public Pessoa Pessoa { get; set; } = null!;

    /// <summary>
    /// Dia do mês (1–31) em que a ocorrência cai. Em meses mais curtos (ex.: fevereiro),
    /// o motor de projeção faz o clamp para o último dia do mês — ver MotorProjecaoFixas.
    /// </summary>
    public int DiaDoMes { get; set; }

    /// <summary>A partir de quando a recorrência vale (inclusive).</summary>
    public DateOnly DataInicio { get; set; }

    /// <summary>Até quando a recorrência vale (inclusive); null significa sem data de fim.</summary>
    public DateOnly? DataFim { get; set; }
}
