using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Tests.Services;

/// <summary>
/// Testa o motor de projeção isoladamente — sem banco, sem InMemory, só dados em memória —
/// porque é uma função pura (ver MotorProjecaoFixas).
/// </summary>
public class MotorProjecaoFixasTests
{
    [Fact]
    public void Projetar_FixaDiaCinco_TresMeses_GeraUmaOcorrenciaPorMes()
    {
        var fixa = NovaFixa(diaDoMes: 5, dataInicio: new DateOnly(2026, 1, 1));

        var ocorrencias = MotorProjecaoFixas.Projetar([fixa], new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));

        Assert.Equal(3, ocorrencias.Count);
        Assert.Equal(
            [new DateOnly(2026, 1, 5), new DateOnly(2026, 2, 5), new DateOnly(2026, 3, 5)],
            ocorrencias.Select(o => o.Data));
    }

    [Fact]
    public void Projetar_DiaDoMesTrintaEUm_EmFevereiro_FazClampParaOUltimoDia()
    {
        // 2026 não é bissexto: fevereiro tem 28 dias.
        var fixa = NovaFixa(diaDoMes: 31, dataInicio: new DateOnly(2026, 1, 1));

        var ocorrencias = MotorProjecaoFixas.Projetar([fixa], new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        Assert.Single(ocorrencias);
        Assert.Equal(new DateOnly(2026, 2, 28), ocorrencias[0].Data);
    }

    [Fact]
    public void Projetar_DiaDoMesTrintaEUm_EmFevereiroBissexto_FazClampParaOVinteENove()
    {
        var fixa = NovaFixa(diaDoMes: 31, dataInicio: new DateOnly(2028, 1, 1));

        var ocorrencias = MotorProjecaoFixas.Projetar([fixa], new DateOnly(2028, 2, 1), new DateOnly(2028, 2, 29));

        Assert.Single(ocorrencias);
        Assert.Equal(new DateOnly(2028, 2, 29), ocorrencias[0].Data);
    }

    [Fact]
    public void Projetar_AntesDaDataInicio_NaoGeraOcorrencia()
    {
        var fixa = NovaFixa(diaDoMes: 10, dataInicio: new DateOnly(2026, 3, 1));

        var ocorrencias = MotorProjecaoFixas.Projetar([fixa], new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28));

        Assert.Empty(ocorrencias);
    }

    [Fact]
    public void Projetar_AposDataFim_ParaDeGerarOcorrencias()
    {
        var fixa = NovaFixa(diaDoMes: 10, dataInicio: new DateOnly(2026, 1, 1), dataFim: new DateOnly(2026, 2, 15));

        var ocorrencias = MotorProjecaoFixas.Projetar([fixa], new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 30));

        // Fevereiro (dia 10) ainda está dentro da vigência (DataFim = 15/02); março não.
        Assert.Equal(2, ocorrencias.Count);
        Assert.Equal([new DateOnly(2026, 1, 10), new DateOnly(2026, 2, 10)], ocorrencias.Select(o => o.Data));
    }

    [Fact]
    public void Projetar_OcorrenciaAntesDoInicioDoIntervalo_EExcluida()
    {
        // Fixa vigente desde antes de "de", mas o dia clampado do primeiro mês do intervalo
        // cai antes de "de" (intervalo começa no meio do mês) — não deve entrar no resultado.
        var fixa = NovaFixa(diaDoMes: 5, dataInicio: new DateOnly(2025, 1, 1));

        var ocorrencias = MotorProjecaoFixas.Projetar([fixa], new DateOnly(2026, 1, 10), new DateOnly(2026, 2, 28));

        Assert.Single(ocorrencias);
        Assert.Equal(new DateOnly(2026, 2, 5), ocorrencias[0].Data);
    }

    private static Fixa NovaFixa(int diaDoMes, DateOnly dataInicio, DateOnly? dataFim = null) => new()
    {
        Id = Guid.NewGuid(),
        Descricao = "Aluguel",
        Valor = 1200m,
        Tipo = TipoTransacao.Despesa,
        PessoaId = Guid.NewGuid(),
        DiaDoMes = diaDoMes,
        DataInicio = dataInicio,
        DataFim = dataFim
    };
}
