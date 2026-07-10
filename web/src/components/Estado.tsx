/** Componentes de estado reutilizados em todas as telas: carregando, erro e vazio. */

export function Carregando({ texto = 'Carregando...' }: { texto?: string }) {
  return (
    <div className="flex items-center gap-2 py-8 text-slate-500 dark:text-slate-400">
      <span
        aria-hidden
        className="h-4 w-4 animate-spin rounded-full border-2 border-slate-300 border-t-slate-600 dark:border-slate-600 dark:border-t-slate-300"
      />
      {texto}
    </div>
  )
}

export function MensagemErro({ mensagem }: { mensagem: string }) {
  return (
    <div
      role="alert"
      className="rounded-md border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950 dark:text-red-300"
    >
      {mensagem}
    </div>
  )
}

export function MensagemVazia({ mensagem }: { mensagem: string }) {
  return <p className="py-8 text-center text-slate-500 dark:text-slate-400">{mensagem}</p>
}
