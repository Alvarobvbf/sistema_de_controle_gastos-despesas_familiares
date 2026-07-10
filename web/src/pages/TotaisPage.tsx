import { useApiData } from '../hooks/useApiData'
import { obterTotais } from '../api/totais'
import { Carregando, MensagemErro, MensagemVazia } from '../components/Estado'
import { formatarMoeda } from '../utils/formatarMoeda'

/** Verde para saldo positivo/zero, vermelho para negativo — mesma lógica usada nas duas linhas. */
function classeSaldo(valor: number): string {
  return valor < 0
    ? 'font-semibold text-red-600 dark:text-red-400'
    : 'font-semibold text-emerald-600 dark:text-emerald-400'
}

export function TotaisPage() {
  const { dados: totais, carregando, erro } = useApiData(() => obterTotais(), [])

  return (
    <div className="flex flex-col gap-4">
      <h2 className="text-xl font-semibold">Totais</h2>

      {carregando && <Carregando />}
      {erro && <MensagemErro mensagem={erro} />}

      {!carregando && !erro && totais && totais.pessoas.length === 0 && (
        <MensagemVazia mensagem="Nenhuma pessoa cadastrada ainda." />
      )}

      {!carregando && !erro && totais && totais.pessoas.length > 0 && (
        <div className="overflow-x-auto rounded-md border border-slate-200 dark:border-slate-800">
          <table className="w-full min-w-[560px] text-left text-sm">
            <thead className="bg-slate-100 dark:bg-slate-800">
              <tr>
                <th className="px-4 py-2 font-medium">Pessoa</th>
                <th className="px-4 py-2 font-medium">Receitas</th>
                <th className="px-4 py-2 font-medium">Despesas</th>
                <th className="px-4 py-2 font-medium">Saldo</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
              {/* Pessoas sem transação vêm do back com 0/0/0 — não são filtradas, aparecem normalmente. */}
              {totais.pessoas.map((pessoa) => (
                <tr key={pessoa.pessoaId}>
                  <td className="px-4 py-2.5">{pessoa.nome}</td>
                  <td className="px-4 py-2.5 text-emerald-600 dark:text-emerald-400">
                    {formatarMoeda(pessoa.totalReceitas)}
                  </td>
                  <td className="px-4 py-2.5 text-red-600 dark:text-red-400">
                    {formatarMoeda(pessoa.totalDespesas)}
                  </td>
                  <td className={`px-4 py-2.5 ${classeSaldo(pessoa.saldo)}`}>{formatarMoeda(pessoa.saldo)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot className="border-t-2 border-slate-300 bg-slate-50 dark:border-slate-700 dark:bg-slate-800/50">
              <tr>
                <td className="px-4 py-3 font-semibold">Total geral</td>
                <td className="px-4 py-3 font-semibold text-emerald-600 dark:text-emerald-400">
                  {formatarMoeda(totais.totalGeral.totalReceitas)}
                </td>
                <td className="px-4 py-3 font-semibold text-red-600 dark:text-red-400">
                  {formatarMoeda(totais.totalGeral.totalDespesas)}
                </td>
                <td className={`px-4 py-3 ${classeSaldo(totais.totalGeral.saldoLiquido)}`}>
                  {formatarMoeda(totais.totalGeral.saldoLiquido)}
                </td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </div>
  )
}
