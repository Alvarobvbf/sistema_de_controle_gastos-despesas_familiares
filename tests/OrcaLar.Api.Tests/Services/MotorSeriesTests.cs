using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Dtos;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Tests.Services;

/// <summary>
/// Testa o agrupamento em buckets isoladamente — sem banco — recebendo "hoje" por
/// parâmetro, o que torna o teste determinístico independentemente de quando ele rodar.
/// </summary>
public class MotorSeriesTests
{
    [Fact]
    public void Calcular_SaldoAcumulado_ContinuaDoHistoricoParaAProjecao()
    {
        var hoje = new DateOnly(2026, 3, 15);
        var transacoes = new List<Transacao>
        {
            NovaTransacao(new DateOnly(2026, 2, 1), 1000m, TipoTransacao.Receita)
        };
        var ocorrencias = new List<OcorrenciaFixa>
        {
            new(new DateOnly(2026, 4, 5), 300m, TipoTransacao.Despesa, Guid.NewGuid(), Guid.NewGuid())
        };

        var pontos = MotorSeries.Calcular(transacoes, ocorrencias, hoje, mesesProjecao: 2, Granularidade.Mes);

        // Histórico: fevereiro (mês do primeiro registro) até março (mês de "hoje").
        // Projeção: março (mês de "hoje") até maio (hoje + 2 meses) — meses contínuos.
        Assert.Equal(
            ["2026-02", "2026-03", "2026-03", "2026-04", "2026-05"],
            pontos.Select(p => p.Periodo));

        Assert.Equal([false, false, true, true, true], pontos.Select(p => p.Projetado));

        // Saldo acumulado: 1000 (receita de fevereiro) segue igual em março (histórico),
        // continua igual em março (projeção, sem evento), cai para 700 em abril (despesa
        // projetada de 300) e se mantém em maio.
        Assert.Equal([1000m, 1000m, 1000m, 700m, 700m], pontos.Select(p => p.SaldoAcumulado));
    }

    [Fact]
    public void Calcular_GranularidadeDia_SoEmiteDiasComEvento()
    {
        var hoje = new DateOnly(2026, 1, 10);
        var transacoes = new List<Transacao>
        {
            NovaTransacao(new DateOnly(2026, 1, 1), 500m, TipoTransacao.Receita),
            NovaTransacao(new DateOnly(2026, 1, 5), 100m, TipoTransacao.Despesa)
        };

        var pontos = MotorSeries.Calcular(transacoes, [], hoje, mesesProjecao: 0, Granularidade.Dia);

        // Só os dois dias com evento real — nenhum dos outros 8 dias entre o primeiro
        // registro e "hoje" é materializado, e a projeção (sem fixas) não gera nada.
        Assert.Equal(["2026-01-01", "2026-01-05"], pontos.Select(p => p.Periodo));
        Assert.Equal([500m, 400m], pontos.Select(p => p.SaldoAcumulado));
    }

    [Fact]
    public void Calcular_SemTransacoesReais_ProjecaoComecaComSaldoBaseZero()
    {
        var hoje = new DateOnly(2026, 6, 1);
        var ocorrencias = new List<OcorrenciaFixa>
        {
            new(new DateOnly(2026, 6, 15), 200m, TipoTransacao.Receita, Guid.NewGuid(), Guid.NewGuid())
        };

        var pontos = MotorSeries.Calcular([], ocorrencias, hoje, mesesProjecao: 0, Granularidade.Mes);

        var ponto = Assert.Single(pontos);
        Assert.Equal("2026-06", ponto.Periodo);
        Assert.True(ponto.Projetado);
        Assert.Equal(200m, ponto.SaldoAcumulado);
    }

    private static Transacao NovaTransacao(DateOnly data, decimal valor, TipoTransacao tipo) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Transação de teste",
        Valor = valor,
        Tipo = tipo,
        PessoaId = Guid.NewGuid(),
        Data = data
    };
}
