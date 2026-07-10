using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;
using OrcaLar.Api.Dtos;

namespace OrcaLar.Api.Services;

/// <summary>
/// Orquestra a série do dashboard: busca os dados reais (transações e fixas) e delega todo
/// o cálculo — expansão de fixas e agrupamento em buckets — aos motores puros
/// (MotorProjecaoFixas, MotorSeries). Este Service não altera /api/totais nem
/// /api/transacoes, que continuam refletindo apenas dados reais.
/// </summary>
public class DashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SerieResponse> ObterSeriesAsync(Granularidade granularidade, int mesesProjecao)
    {
        // "Hoje" é sempre o relógio do servidor — mesmo critério de Data em Transacao.
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);

        var transacoes = await _dbContext.Transacoes.AsNoTracking().ToListAsync();
        var fixas = await _dbContext.Fixas.AsNoTracking().ToListAsync();

        var fimProjecao = hoje.AddMonths(mesesProjecao);
        var ocorrenciasProjetadas = MotorProjecaoFixas.Projetar(fixas, hoje, fimProjecao);

        var pontos = MotorSeries.Calcular(transacoes, ocorrenciasProjetadas, hoje, mesesProjecao, granularidade);

        return new SerieResponse(granularidade, pontos);
    }
}
