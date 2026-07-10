import type { TipoTransacao } from '../types/api'

const estilos: Record<TipoTransacao, string> = {
  Receita: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300',
  Despesa: 'bg-red-100 text-red-700 dark:bg-red-950 dark:text-red-300',
}

/** Selo visual do tipo de transação — receita em verde, despesa em vermelho. */
export function BadgeTipo({ tipo }: { tipo: TipoTransacao }) {
  return <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${estilos[tipo]}`}>{tipo}</span>
}
