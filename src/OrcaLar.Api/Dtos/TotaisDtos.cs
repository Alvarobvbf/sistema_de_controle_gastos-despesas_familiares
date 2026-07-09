namespace OrcaLar.Api.Dtos;

/// <summary>Totais de uma pessoa: soma de receitas, soma de despesas e o saldo (receitas − despesas).</summary>
public record PessoaTotalDto(Guid PessoaId, string Nome, decimal TotalReceitas, decimal TotalDespesas, decimal Saldo);

/// <summary>Totais somados de todas as pessoas cadastradas.</summary>
public record TotalGeralDto(decimal TotalReceitas, decimal TotalDespesas, decimal SaldoLiquido);

/// <summary>Resposta de GET /api/totais: totais por pessoa + total geral consolidado.</summary>
public record TotaisResponse(IReadOnlyList<PessoaTotalDto> Pessoas, TotalGeralDto TotalGeral);
