using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Dtos;

namespace OrcaLar.Api.Services;

/// <summary>
/// Calcula os totais de receitas, despesas e saldo por pessoa, e o total geral consolidado.
/// </summary>
public class TotaisService
{
    private readonly AppDbContext _dbContext;

    public TotaisService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TotaisResponse> ObterAsync()
    {
        // Include(p => p.Transacoes) traz todas as pessoas, mesmo as sem nenhuma transação —
        // Sum() sobre uma coleção vazia retorna 0, então elas aparecem naturalmente como 0/0/0
        // em vez de sumir da listagem.
        var pessoas = await _dbContext.Pessoas
            .Include(p => p.Transacoes)
            .AsNoTracking()
            .ToListAsync();

        var totaisPorPessoa = pessoas
            .Select(CalcularTotalDaPessoa)
            .OrderBy(p => p.Nome)
            .ToList();

        var totalGeral = new TotalGeralDto(
            TotalReceitas: totaisPorPessoa.Sum(p => p.TotalReceitas),
            TotalDespesas: totaisPorPessoa.Sum(p => p.TotalDespesas),
            SaldoLiquido: totaisPorPessoa.Sum(p => p.Saldo));

        return new TotaisResponse(totaisPorPessoa, totalGeral);
    }

    private static PessoaTotalDto CalcularTotalDaPessoa(Pessoa pessoa)
    {
        var totalReceitas = pessoa.Transacoes
            .Where(t => t.Tipo == TipoTransacao.Receita)
            .Sum(t => t.Valor);

        var totalDespesas = pessoa.Transacoes
            .Where(t => t.Tipo == TipoTransacao.Despesa)
            .Sum(t => t.Valor);

        return new PessoaTotalDto(pessoa.Id, pessoa.Nome, totalReceitas, totalDespesas, totalReceitas - totalDespesas);
    }
}
