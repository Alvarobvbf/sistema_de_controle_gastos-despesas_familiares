using OrcaLar.Api.Domain.Entities;

namespace OrcaLar.Api.Services;

/// <summary>Uma ocorrência virtual de uma Fixa em uma data concreta — nunca persistida.</summary>
public record OcorrenciaFixa(DateOnly Data, decimal Valor, TipoTransacao Tipo, Guid PessoaId, Guid FixaId);

/// <summary>
/// Expande regras de Fixa em ocorrências virtuais dentro de um intervalo. Função pura e
/// determinística: não lê o relógio do sistema nem o banco — recebe tudo por parâmetro, o
/// que a torna 100% testável isoladamente (sem EF, sem InMemory, sem mocks).
/// </summary>
public static class MotorProjecaoFixas
{
    /// <summary>
    /// Para cada Fixa, gera uma ocorrência por mês do intervalo [de, ate] em que ela estiver
    /// vigente (DataInicio/DataFim), com o dia já ajustado (clamp) para o mês em questão.
    /// </summary>
    public static IReadOnlyList<OcorrenciaFixa> Projetar(IEnumerable<Fixa> fixas, DateOnly de, DateOnly ate)
    {
        var ocorrencias = new List<OcorrenciaFixa>();

        foreach (var fixa in fixas)
        {
            foreach (var data in GerarOcorrencias(fixa, de, ate))
            {
                ocorrencias.Add(new OcorrenciaFixa(data, fixa.Valor, fixa.Tipo, fixa.PessoaId, fixa.Id));
            }
        }

        return ocorrencias;
    }

    private static IEnumerable<DateOnly> GerarOcorrencias(Fixa fixa, DateOnly de, DateOnly ate)
    {
        var anoCursor = de.Year;
        var mesCursor = de.Month;

        // Itera mês a mês, do mês de "de" até o mês de "ate" (inclusive), gerando no máximo
        // uma ocorrência por mês do intervalo.
        while (new DateOnly(anoCursor, mesCursor, 1) <= ate)
        {
            // Clamp: se o mês não tem o DiaDoMes pedido (ex.: 31 em abril, ou 29/30/31 em
            // fevereiro), a ocorrência cai no último dia do mês em vez de estourar a data.
            var ultimoDiaDoMes = DateTime.DaysInMonth(anoCursor, mesCursor);
            var dia = Math.Min(fixa.DiaDoMes, ultimoDiaDoMes);
            var ocorrencia = new DateOnly(anoCursor, mesCursor, dia);

            var dentroDaVigencia = ocorrencia >= fixa.DataInicio && (fixa.DataFim is null || ocorrencia <= fixa.DataFim);
            var dentroDoIntervalo = ocorrencia >= de && ocorrencia <= ate;

            if (dentroDaVigencia && dentroDoIntervalo)
            {
                yield return ocorrencia;
            }

            mesCursor++;
            if (mesCursor > 12)
            {
                mesCursor = 1;
                anoCursor++;
            }
        }
    }
}
