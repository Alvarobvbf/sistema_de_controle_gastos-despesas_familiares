import { useMemo, useState } from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { useApiData } from '../hooks/useApiData'
import { obterSeries } from '../api/dashboard'
import { Carregando, MensagemErro, MensagemVazia } from './Estado'
import { formatarMoeda } from '../utils/formatarMoeda'
import { formatarPeriodo } from '../utils/formatarData'
import type { Granularidade } from '../types/api'

const OPCOES_HORIZONTE = [3, 6, 12] as const

/** Verde para saldo positivo/zero, vermelho para negativo — mesma convenção da tabela de totais. */
function classeSaldo(valor: number): string {
  return valor < 0
    ? 'font-semibold text-red-600 dark:text-red-400'
    : 'font-semibold text-emerald-600 dark:text-emerald-400'
}

export function Dashboard() {
  const [granularidade, setGranularidade] = useState<Granularidade>('Mes')
  const [mesesProjecao, setMesesProjecao] = useState(6)

  const { dados: serie, carregando, erro } = useApiData(
    () => obterSeries({ granularidade, mesesProjecao }),
    [granularidade, mesesProjecao],
  )

  // Monta o dataset dos gráficos a partir dos pontos do back. A técnica para a linha de
  // saldo acumulado "sólido até hoje, pontilhado depois" é: duas séries (saldoRealizado e
  // saldoProjetado) sobre o MESMO array, cada uma só preenchida nos pontos do seu tipo
  // (null no resto — o Recharts não desenha segmento onde o valor é null). O último ponto
  // histórico também recebe o valor na série projetada, servindo de vértice compartilhado
  // que conecta visualmente as duas linhas exatamente onde o realizado termina.
  const dados = useMemo(() => {
    const pontos = serie?.pontos ?? []
    const linhas = pontos.map((ponto) => ({
      periodo: ponto.periodo,
      receitas: ponto.receitas,
      despesas: ponto.despesas,
      projetado: ponto.projetado,
      saldoRealizado: ponto.projetado ? null : ponto.saldoAcumulado,
      saldoProjetado: ponto.projetado ? ponto.saldoAcumulado : null,
    }))

    const ultimoIndiceHistorico = linhas.findLastIndex((linha) => !linha.projetado)
    if (ultimoIndiceHistorico >= 0 && ultimoIndiceHistorico < linhas.length - 1) {
      linhas[ultimoIndiceHistorico].saldoProjetado = linhas[ultimoIndiceHistorico].saldoRealizado
    }

    return linhas
  }, [serie])

  const saldoAtual = useMemo(() => {
    const ultimoRealizado = [...dados].reverse().find((d) => d.saldoRealizado !== null)
    return ultimoRealizado?.saldoRealizado ?? 0
  }, [dados])

  const saldoProjetadoFinal = dados.at(-1)?.saldoProjetado ?? dados.at(-1)?.saldoRealizado ?? 0

  return (
    <section className="mt-10 flex flex-col gap-4 border-t border-slate-200 pt-8 dark:border-slate-800">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xl font-semibold">Dashboard</h2>
        <div className="flex flex-wrap items-center gap-3">
          <div className="flex overflow-hidden rounded-md border border-slate-300 text-sm dark:border-slate-700">
            <button
              type="button"
              onClick={() => setGranularidade('Mes')}
              className={`px-3 py-1.5 font-medium transition-colors ${
                granularidade === 'Mes'
                  ? 'bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900'
                  : 'bg-transparent hover:bg-slate-100 dark:hover:bg-slate-800'
              }`}
            >
              Mês
            </button>
            <button
              type="button"
              onClick={() => setGranularidade('Dia')}
              className={`px-3 py-1.5 font-medium transition-colors ${
                granularidade === 'Dia'
                  ? 'bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900'
                  : 'bg-transparent hover:bg-slate-100 dark:hover:bg-slate-800'
              }`}
            >
              Dia
            </button>
          </div>
          <label className="flex items-center gap-2 text-sm">
            Projetar
            <select
              value={mesesProjecao}
              onChange={(evento) => setMesesProjecao(Number(evento.target.value))}
              className="rounded-md border border-slate-300 px-2 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            >
              {OPCOES_HORIZONTE.map((meses) => (
                <option key={meses} value={meses}>
                  {meses} meses
                </option>
              ))}
            </select>
          </label>
        </div>
      </div>

      {carregando && <Carregando />}
      {erro && <MensagemErro mensagem={erro} />}
      {!carregando && !erro && dados.length === 0 && (
        <MensagemVazia mensagem="Sem dados suficientes para montar o dashboard ainda." />
      )}

      {!carregando && !erro && dados.length > 0 && (
        <>
          <div className="flex flex-wrap gap-6 text-sm">
            <span>
              Saldo realizado (hoje): <span className={classeSaldo(saldoAtual)}>{formatarMoeda(saldoAtual)}</span>
            </span>
            <span>
              Saldo projetado (+{mesesProjecao} meses):{' '}
              <span className={classeSaldo(saldoProjetadoFinal)}>{formatarMoeda(saldoProjetadoFinal)}</span>
            </span>
          </div>

          <div>
            <h3 className="mb-2 text-sm font-medium text-slate-600 dark:text-slate-300">
              Saldo acumulado — realizado (sólido) × projetado (pontilhado)
            </h3>
            <ResponsiveContainer width="100%" height={280}>
              <LineChart data={dados} margin={{ left: 8, right: 16 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-slate-200 dark:stroke-slate-700" />
                <XAxis dataKey="periodo" tickFormatter={formatarPeriodo} tick={{ fontSize: 12 }} />
                <YAxis tickFormatter={(valor: number) => formatarMoeda(valor)} width={90} tick={{ fontSize: 12 }} />
                <Tooltip
                  formatter={(valor) => formatarMoeda(Number(valor))}
                  labelFormatter={(rotulo) => formatarPeriodo(String(rotulo))}
                />
                <Legend />
                <ReferenceLine y={0} stroke="currentColor" className="text-slate-400 dark:text-slate-600" />
                <Line
                  type="monotone"
                  dataKey="saldoRealizado"
                  name="Realizado"
                  stroke="#0f172a"
                  strokeWidth={2}
                  dot={false}
                  connectNulls={false}
                  isAnimationActive={false}
                />
                <Line
                  type="monotone"
                  dataKey="saldoProjetado"
                  name="Projetado (fixas)"
                  stroke="#0f172a"
                  strokeWidth={2}
                  strokeDasharray="6 4"
                  dot={false}
                  connectNulls={false}
                  isAnimationActive={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div>
            <h3 className="mb-2 text-sm font-medium text-slate-600 dark:text-slate-300">
              Receitas × despesas por período{' '}
              <span className="font-normal text-slate-400 dark:text-slate-500">
                (períodos projetados em tom mais claro)
              </span>
            </h3>
            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={dados} margin={{ left: 8, right: 16 }}>
                <CartesianGrid strokeDasharray="3 3" className="stroke-slate-200 dark:stroke-slate-700" />
                <XAxis dataKey="periodo" tickFormatter={formatarPeriodo} tick={{ fontSize: 12 }} />
                <YAxis tickFormatter={(valor: number) => formatarMoeda(valor)} width={90} tick={{ fontSize: 12 }} />
                <Tooltip
                  formatter={(valor) => formatarMoeda(Number(valor))}
                  labelFormatter={(rotulo, payload) => {
                    const primeiraEntrada = payload?.[0]?.payload as { projetado?: boolean } | undefined
                    return `${formatarPeriodo(String(rotulo))}${primeiraEntrada?.projetado ? ' (projetado)' : ''}`
                  }}
                />
                <Legend />
                <Bar dataKey="receitas" name="Receitas" fill="#10b981">
                  {dados.map((linha, indice) => (
                    <Cell key={`receita-${indice}`} fillOpacity={linha.projetado ? 0.45 : 1} />
                  ))}
                </Bar>
                <Bar dataKey="despesas" name="Despesas" fill="#ef4444">
                  {dados.map((linha, indice) => (
                    <Cell key={`despesa-${indice}`} fillOpacity={linha.projetado ? 0.45 : 1} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </>
      )}
    </section>
  )
}
