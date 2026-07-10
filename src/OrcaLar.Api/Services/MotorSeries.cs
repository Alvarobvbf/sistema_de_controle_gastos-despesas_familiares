using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Dtos;

namespace OrcaLar.Api.Services;

/// <summary>
/// Agrupa transações reais e ocorrências projetadas em buckets de série temporal. Função
/// pura: recebe os dados já carregados e a data de "hoje" por parâmetro (nunca lê o
/// relógio do sistema), o que a torna testável sem banco de dados.
/// </summary>
public static class MotorSeries
{
    private readonly record struct Evento(DateOnly Data, decimal Valor, TipoTransacao Tipo);

    public static IReadOnlyList<SeriePontoDto> Calcular(
        IReadOnlyList<Transacao> transacoesReais,
        IReadOnlyList<OcorrenciaFixa> ocorrenciasProjetadas,
        DateOnly hoje,
        int mesesProjecao,
        Granularidade granularidade)
    {
        var pontos = new List<SeriePontoDto>();
        var saldoAcumulado = 0m;

        // Histórico: só transações reais, do primeiro registro até hoje. Sem transações,
        // não há histórico algum — a série começa direto na projeção, com saldo base 0.
        if (transacoesReais.Count > 0)
        {
            var primeiraData = transacoesReais.Min(t => t.Data);
            var eventos = transacoesReais.Select(t => new Evento(t.Data, t.Valor, t.Tipo));
            var buckets = AgruparPorPeriodo(eventos, primeiraData, hoje, granularidade);

            foreach (var bucket in buckets)
            {
                var saldoPeriodo = bucket.Receitas - bucket.Despesas;
                saldoAcumulado += saldoPeriodo;
                pontos.Add(new SeriePontoDto(bucket.Periodo, bucket.Receitas, bucket.Despesas, saldoPeriodo, saldoAcumulado, Projetado: false));
            }
        }

        // Projeção: ocorrências virtuais de Fixas, de hoje até hoje + mesesProjecao. O saldo
        // acumulado continua exatamente de onde o histórico parou (ou de 0, sem histórico) —
        // é isso que faz a série ser contínua na transição realizado → projetado.
        var fimProjecao = hoje.AddMonths(mesesProjecao);
        var eventosProjetados = ocorrenciasProjetadas.Select(o => new Evento(o.Data, o.Valor, o.Tipo));
        var bucketsProjecao = AgruparPorPeriodo(eventosProjetados, hoje, fimProjecao, granularidade);

        foreach (var bucket in bucketsProjecao)
        {
            var saldoPeriodo = bucket.Receitas - bucket.Despesas;
            saldoAcumulado += saldoPeriodo;
            pontos.Add(new SeriePontoDto(bucket.Periodo, bucket.Receitas, bucket.Despesas, saldoPeriodo, saldoAcumulado, Projetado: true));
        }

        return pontos;
    }

    private static string ChaveDoPeriodo(DateOnly data, Granularidade granularidade) => granularidade == Granularidade.Dia
        ? data.ToString("yyyy-MM-dd")
        : data.ToString("yyyy-MM");

    private static List<(string Periodo, decimal Receitas, decimal Despesas)> AgruparPorPeriodo(
        IEnumerable<Evento> eventos, DateOnly de, DateOnly ate, Granularidade granularidade)
    {
        var porPeriodo = eventos
            .GroupBy(e => ChaveDoPeriodo(e.Data, granularidade))
            .ToDictionary(
                g => g.Key,
                g => (
                    Receitas: g.Where(e => e.Tipo == TipoTransacao.Receita).Sum(e => e.Valor),
                    Despesas: g.Where(e => e.Tipo == TipoTransacao.Despesa).Sum(e => e.Valor)));

        if (granularidade == Granularidade.Dia)
        {
            // Diário: só dias com evento (real ou projetado) — não materializa um bucket
            // vazio para cada um dos ~365 dias do intervalo.
            return porPeriodo
                .OrderBy(kv => kv.Key)
                .Select(kv => (kv.Key, kv.Value.Receitas, kv.Value.Despesas))
                .ToList();
        }

        // Mensal: contínuo — emite todo mês de [de, ate], mesmo os sem nenhum evento, para
        // que o gráfico não tenha buracos.
        var resultado = new List<(string, decimal, decimal)>();
        var cursor = new DateOnly(de.Year, de.Month, 1);
        var limite = new DateOnly(ate.Year, ate.Month, 1);

        while (cursor <= limite)
        {
            var chave = ChaveDoPeriodo(cursor, granularidade);
            var valores = porPeriodo.TryGetValue(chave, out var v) ? v : (Receitas: 0m, Despesas: 0m);
            resultado.Add((chave, valores.Receitas, valores.Despesas));
            cursor = cursor.AddMonths(1);
        }

        return resultado;
    }
}
