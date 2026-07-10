const formatador = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

/** Formata um número como moeda brasileira (R$ 1.234,56). */
export function formatarMoeda(valor: number): string {
  return formatador.format(valor)
}
