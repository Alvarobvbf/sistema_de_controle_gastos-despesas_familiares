/**
 * Formata uma data "yyyy-MM-dd" (formato do DateOnly do back e do <input type="date">)
 * para "dd/mm/aaaa". Manipula a string diretamente em vez de `new Date(...)` para não
 * sofrer conversão de fuso horário (new Date("yyyy-MM-dd") é interpretada como UTC meia-noite,
 * o que pode exibir o dia anterior dependendo do fuso do navegador).
 */
export function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-')
  return `${dia}/${mes}/${ano}`
}

/**
 * Formata o rótulo de período do dashboard: "yyyy-MM" vira "MM/aaaa" (granularidade mês),
 * "yyyy-MM-dd" vira "dd/MM" (granularidade dia) — mais curto que a data completa, o
 * suficiente para diferenciar pontos vizinhos no eixo do gráfico.
 */
export function formatarPeriodo(periodo: string): string {
  const partes = periodo.split('-')
  if (partes.length === 3) {
    const [, mes, dia] = partes
    return `${dia}/${mes}`
  }
  const [ano, mes] = partes
  return `${mes}/${ano}`
}
