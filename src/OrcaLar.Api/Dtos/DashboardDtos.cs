namespace OrcaLar.Api.Dtos;

/// <summary>Granularidade de agrupamento da série do dashboard.</summary>
public enum Granularidade
{
    Mes,
    Dia
}

/// <summary>
/// Um ponto da série do dashboard. "Periodo" é o rótulo do bucket ("YYYY-MM" para
/// granularidade mensal, "YYYY-MM-DD" para diária). "Projetado" indica se os valores vêm de
/// transações reais (false) ou de ocorrências projetadas de Fixas (true).
/// </summary>
public record SeriePontoDto(
    string Periodo,
    decimal Receitas,
    decimal Despesas,
    decimal SaldoPeriodo,
    decimal SaldoAcumulado,
    bool Projetado
);

/// <summary>Resposta de GET /api/dashboard/series.</summary>
public record SerieResponse(Granularidade Granularidade, IReadOnlyList<SeriePontoDto> Pontos);
