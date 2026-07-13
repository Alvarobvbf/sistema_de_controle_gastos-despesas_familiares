import { useMemo, useState, type FormEvent } from 'react'
import { useApiData } from '../hooks/useApiData'
import { listarPessoas } from '../api/pessoas'
import { listarFixas, criarFixa, deletarFixa } from '../api/fixas'
import { ApiError } from '../api/client'
import { Carregando, MensagemErro, MensagemVazia } from '../components/Estado'
import { BadgeTipo } from '../components/BadgeTipo'
import { formatarMoeda } from '../utils/formatarMoeda'
import { formatarData } from '../utils/formatarData'
import type { TipoTransacao } from '../types/api'

/** Espelha a regra do back (RegrasLancamento): abaixo disso, só despesa é permitida. */
const IDADE_MINIMA_RECEITA = 18

export function FixasPage() {
  const { dados: pessoas, erro: erroPessoas } = useApiData(() => listarPessoas(), [])

  const [pessoaIdFiltro, setPessoaIdFiltro] = useState('')

  const {
    dados: fixas,
    carregando,
    erro,
    recarregar,
  } = useApiData(() => listarFixas(pessoaIdFiltro || undefined), [pessoaIdFiltro])

  const [descricao, setDescricao] = useState('')
  const [valor, setValor] = useState('')
  const [tipo, setTipo] = useState<TipoTransacao>('Despesa')
  const [pessoaId, setPessoaId] = useState('')
  const [diaDoMes, setDiaDoMes] = useState('')
  const [dataInicio, setDataInicio] = useState('')
  const [dataFim, setDataFim] = useState('')
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  const pessoaSelecionada = useMemo(
    () => pessoas?.find((pessoa) => pessoa.id === pessoaId),
    [pessoas, pessoaId],
  )
  const pessoaEhMenorDeIdade = pessoaSelecionada !== undefined && pessoaSelecionada.idade < IDADE_MINIMA_RECEITA

  function aoSelecionarPessoa(id: string) {
    setPessoaId(id)
    const pessoa = pessoas?.find((p) => p.id === id)
    // Mesma UX preventiva de Transações: evita montar um pedido que o back vai rejeitar
    // (422 REGRA_MENOR_RECEITA). O back continua sendo a autoridade da regra.
    if (pessoa && pessoa.idade < IDADE_MINIMA_RECEITA) {
      setTipo('Despesa')
    }
  }

  async function aoSubmeter(evento: FormEvent) {
    evento.preventDefault()
    setErroFormulario(null)

    const descricaoAparada = descricao.trim()
    const valorNumero = Number(valor)
    const diaDoMesNumero = Number(diaDoMes)

    if (!descricaoAparada) {
      setErroFormulario('Informe a descrição.')
      return
    }
    if (valor.trim() === '' || Number.isNaN(valorNumero) || valorNumero <= 0) {
      setErroFormulario('Informe um valor maior que zero.')
      return
    }
    if (!pessoaId) {
      setErroFormulario('Selecione a pessoa.')
      return
    }
    if (
      diaDoMes.trim() === '' ||
      !Number.isInteger(diaDoMesNumero) ||
      diaDoMesNumero < 1 ||
      diaDoMesNumero > 31
    ) {
      setErroFormulario('Informe um dia do mês entre 1 e 31.')
      return
    }
    if (dataInicio && dataFim && dataFim < dataInicio) {
      setErroFormulario('A data de fim deve ser maior ou igual à data de início.')
      return
    }

    setEnviando(true)
    try {
      await criarFixa({
        descricao: descricaoAparada,
        valor: valorNumero,
        tipo,
        pessoaId,
        diaDoMes: diaDoMesNumero,
        dataInicio: dataInicio || undefined,
        dataFim: dataFim || undefined,
      })
      setDescricao('')
      setValor('')
      setDiaDoMes('')
      setDataInicio('')
      setDataFim('')
      recarregar()
    } catch (erroCapturado) {
      // 422 (REGRA_MENOR_RECEITA / PESSOA_NAO_ENCONTRADA) chega aqui com a message
      // pronta do back — a checagem preventiva acima só evita o caso mais comum.
      setErroFormulario(
        erroCapturado instanceof ApiError ? erroCapturado.message : 'Não foi possível criar a fixa.',
      )
    } finally {
      setEnviando(false)
    }
  }

  async function aoDeletar(id: string, descricaoFixa: string) {
    const confirmado = window.confirm(`Tem certeza que deseja excluir a fixa "${descricaoFixa}"?`)
    if (!confirmado) return

    try {
      await deletarFixa(id)
      recarregar()
    } catch (erroCapturado) {
      window.alert(erroCapturado instanceof ApiError ? erroCapturado.message : 'Não foi possível excluir a fixa.')
    }
  }

  return (
    <div className="flex flex-col gap-8">
      <section>
        <h2 className="text-xl font-semibold">Nova fixa</h2>
        <p className="mb-3 text-sm text-slate-500 dark:text-slate-400">
          Lançamentos que se repetem todo mês; aparecem projetados no dashboard, não na
          lista de transações.
        </p>
        <form onSubmit={aoSubmeter} className="flex flex-wrap items-start gap-3">
          <div className="flex w-48 flex-col gap-1">
            <label htmlFor="descricao" className="text-sm font-medium">
              Descrição
            </label>
            <input
              id="descricao"
              value={descricao}
              onChange={(evento) => setDescricao(evento.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="flex w-28 flex-col gap-1">
            <label htmlFor="valor" className="text-sm font-medium">
              Valor (R$)
            </label>
            <input
              id="valor"
              type="number"
              min={0.01}
              step={0.01}
              value={valor}
              onChange={(evento) => setValor(evento.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="flex w-48 flex-col gap-1">
            <label htmlFor="pessoa" className="text-sm font-medium">
              Pessoa
            </label>
            <select
              id="pessoa"
              value={pessoaId}
              onChange={(evento) => aoSelecionarPessoa(evento.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            >
              <option value="">Selecione...</option>
              {pessoas?.map((pessoa) => (
                <option key={pessoa.id} value={pessoa.id}>
                  {pessoa.nome} ({pessoa.idade} anos)
                </option>
              ))}
            </select>
          </div>
          <div className="flex w-32 flex-col gap-1">
            <label htmlFor="tipo" className="text-sm font-medium">
              Tipo
            </label>
            <select
              id="tipo"
              value={tipo}
              onChange={(evento) => setTipo(evento.target.value as TipoTransacao)}
              className="w-full rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            >
              <option value="Despesa">Despesa</option>
              <option value="Receita" disabled={pessoaEhMenorDeIdade}>
                Receita
              </option>
            </select>
          </div>
          <div className="flex w-24 flex-col gap-1">
            <label htmlFor="diaDoMes" className="text-sm font-medium">
              Dia do mês
            </label>
            <input
              id="diaDoMes"
              type="number"
              min={1}
              max={31}
              step={1}
              value={diaDoMes}
              onChange={(evento) => setDiaDoMes(evento.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="flex w-40 flex-col gap-1">
            <label htmlFor="dataInicio" className="text-sm font-medium">
              Início
            </label>
            <input
              id="dataInicio"
              type="date"
              value={dataInicio}
              onChange={(evento) => setDataInicio(evento.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
            <span className="text-xs text-slate-500 dark:text-slate-400">vazio = hoje</span>
          </div>
          <div className="flex w-40 flex-col gap-1">
            <label htmlFor="dataFim" className="text-sm font-medium">
              Fim (opcional)
            </label>
            <input
              id="dataFim"
              type="date"
              value={dataFim}
              onChange={(evento) => setDataFim(evento.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="flex flex-col gap-1">
            {/* Rótulo invisível: mantém o botão alinhado com a linha dos inputs (que ficam
                logo abaixo do próprio rótulo visível de cada campo). */}
            <span aria-hidden className="invisible text-sm font-medium">
              Ação
            </span>
            <button
              type="submit"
              disabled={enviando}
              className="rounded-md bg-slate-900 px-4 py-1.5 font-medium text-white disabled:opacity-50 dark:bg-slate-100 dark:text-slate-900"
            >
              {enviando ? 'Salvando...' : 'Adicionar'}
            </button>
          </div>
        </form>
        {pessoaEhMenorDeIdade && (
          <p className="mt-2 text-sm text-amber-600 dark:text-amber-400">
            Essa pessoa é menor de 18 anos: só é possível cadastrar despesas fixas.
          </p>
        )}
        {erroFormulario && (
          <div className="mt-3">
            <MensagemErro mensagem={erroFormulario} />
          </div>
        )}
      </section>

      <section>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-xl font-semibold">Fixas cadastradas</h2>
          <select
            value={pessoaIdFiltro}
            onChange={(evento) => setPessoaIdFiltro(evento.target.value)}
            className="rounded-md border border-slate-300 px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
          >
            <option value="">Todas as pessoas</option>
            {pessoas?.map((pessoa) => (
              <option key={pessoa.id} value={pessoa.id}>
                {pessoa.nome}
              </option>
            ))}
          </select>
        </div>

        {erroPessoas && <MensagemErro mensagem={erroPessoas} />}
        {carregando && <Carregando />}
        {erro && <MensagemErro mensagem={erro} />}
        {!carregando && !erro && fixas && fixas.length === 0 && (
          <MensagemVazia mensagem="Nenhuma fixa cadastrada." />
        )}
        {!carregando && !erro && fixas && fixas.length > 0 && (
          <ul className="divide-y divide-slate-200 rounded-md border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
            {fixas.map((fixa) => (
              <li key={fixa.id} className="flex items-center justify-between px-4 py-2.5">
                <div className="flex flex-col">
                  <span className="font-medium">{fixa.descricao}</span>
                  <span className="text-sm text-slate-500 dark:text-slate-400">
                    {fixa.pessoaNome} — todo dia {fixa.diaDoMes}, a partir de{' '}
                    {formatarData(fixa.dataInicio)}
                    {fixa.dataFim && <> até {formatarData(fixa.dataFim)}</>}
                  </span>
                </div>
                <div className="flex items-center gap-3">
                  <BadgeTipo tipo={fixa.tipo} />
                  <span
                    className={
                      fixa.tipo === 'Receita'
                        ? 'font-medium text-emerald-600 dark:text-emerald-400'
                        : 'font-medium text-red-600 dark:text-red-400'
                    }
                  >
                    {formatarMoeda(fixa.valor)}
                  </span>
                  <button
                    type="button"
                    onClick={() => aoDeletar(fixa.id, fixa.descricao)}
                    className="text-sm font-medium text-red-600 hover:underline dark:text-red-400"
                  >
                    Excluir
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
