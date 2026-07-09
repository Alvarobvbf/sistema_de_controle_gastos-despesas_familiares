namespace OrcaLar.Api.Domain.Entities;

/// <summary>
/// Representa uma pessoa do agregado familiar/residência. É o "dono" das transações:
/// ao deletar uma Pessoa, todas as suas Transacoes são apagadas em cascata (ver AppDbContext).
/// </summary>
public class Pessoa
{
    public Guid Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Idade é usada pela regra de negócio de Transacao: pessoas menores de 18 anos
    /// só podem ter Despesas cadastradas, nunca Receitas.
    /// </summary>
    public int Idade { get; set; }

    public ICollection<Transacao> Transacoes { get; set; } = new List<Transacao>();
}
