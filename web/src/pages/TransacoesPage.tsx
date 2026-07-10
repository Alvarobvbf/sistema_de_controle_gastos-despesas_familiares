import { useMemo, useState, type FormEvent } from 'react'
import { useApiData } from '../hooks/useApiData'
import { listarPessoas } from '../api/pessoas'
import { listarTransacoes, criarTransacao } from '../api/transacoes'
import { ApiError } from '../api/client'
import { Carregando, MensagemErro, MensagemVazia } from '../components/Estado'
import { BadgeTipo } from '../components/BadgeTipo'
import { formatarMoeda } from '../utils/formatarMoeda'
import type { TipoTransacao } from '../types/api'

/** Espelha a regra do back (TransacaoService): abaixo disso, só despesa é permitida. */
const IDADE_MINIMA_RECEITA = 18

export function TransacoesPage() {
  // Pessoas cadastradas — alimentam tanto o filtro quanto o select de criação.
  const { dados: pessoas, erro: erroPessoas } = useApiData(() => listarPessoas(), [])

  const [pessoaIdFiltro, setPessoaIdFiltro] = useState('')
  const [tipoFiltro, setTipoFiltro] = useState<TipoTransacao | ''>('')

  const {
    dados: transacoes,
    carregando,
    erro,
    recarregar,
  } = useApiData(
    () => listarTransacoes({ pessoaId: pessoaIdFiltro || undefined, tipo: tipoFiltro || undefined }),
    [pessoaIdFiltro, tipoFiltro],
  )

  const [descricao, setDescricao] = useState('')
  const [valor, setValor] = useState('')
  const [tipo, setTipo] = useState<TipoTransacao>('Despesa')
  const [pessoaId, setPessoaId] = useState('')
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
    // UX preventiva: se a pessoa é menor de idade, já força "Despesa" — evita que o
    // usuário monte um pedido que o back vai rejeitar (422 REGRA_MENOR_RECEITA). O back
    // continua sendo a autoridade: essa checagem só evita o caso comum, não substitui a dele.
    if (pessoa && pessoa.idade < IDADE_MINIMA_RECEITA) {
      setTipo('Despesa')
    }
  }

  async function aoSubmeter(evento: FormEvent) {
    evento.preventDefault()
    setErroFormulario(null)

    const descricaoAparada = descricao.trim()
    const valorNumero = Number(valor)

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

    setEnviando(true)
    try {
      await criarTransacao({ descricao: descricaoAparada, valor: valorNumero, tipo, pessoaId })
      setDescricao('')
      setValor('')
      recarregar()
    } catch (erroCapturado) {
      // 422 (REGRA_MENOR_RECEITA / PESSOA_NAO_ENCONTRADA) chega aqui com a message
      // pronta do back — a checagem preventiva acima só evita o caso mais comum.
      setErroFormulario(
        erroCapturado instanceof ApiError ? erroCapturado.message : 'Não foi possível criar a transação.',
      )
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="flex flex-col gap-8">
      <section>
        <h2 className="mb-3 text-xl font-semibold">Nova transação</h2>
        <form onSubmit={aoSubmeter} className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col gap-1">
            <label htmlFor="descricao" className="text-sm font-medium">
              Descrição
            </label>
            <input
              id="descricao"
              value={descricao}
              onChange={(evento) => setDescricao(evento.target.value)}
              className="rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="flex flex-col gap-1">
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
              className="w-28 rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="pessoa" className="text-sm font-medium">
              Pessoa
            </label>
            <select
              id="pessoa"
              value={pessoaId}
              onChange={(evento) => aoSelecionarPessoa(evento.target.value)}
              className="rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            >
              <option value="">Selecione...</option>
              {pessoas?.map((pessoa) => (
                <option key={pessoa.id} value={pessoa.id}>
                  {pessoa.nome} ({pessoa.idade} anos)
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="tipo" className="text-sm font-medium">
              Tipo
            </label>
            <select
              id="tipo"
              value={tipo}
              onChange={(evento) => setTipo(evento.target.value as TipoTransacao)}
              className="rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            >
              <option value="Despesa">Despesa</option>
              <option value="Receita" disabled={pessoaEhMenorDeIdade}>
                Receita
              </option>
            </select>
          </div>
          <button
            type="submit"
            disabled={enviando}
            className="rounded-md bg-slate-900 px-4 py-1.5 font-medium text-white disabled:opacity-50 dark:bg-slate-100 dark:text-slate-900"
          >
            {enviando ? 'Salvando...' : 'Adicionar'}
          </button>
        </form>
        {pessoaEhMenorDeIdade && (
          <p className="mt-2 text-sm text-amber-600 dark:text-amber-400">
            Essa pessoa é menor de 18 anos: só é possível cadastrar despesas.
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
          <h2 className="text-xl font-semibold">Transações</h2>
          <div className="flex flex-wrap gap-2">
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
            <select
              value={tipoFiltro}
              onChange={(evento) => setTipoFiltro(evento.target.value as TipoTransacao | '')}
              className="rounded-md border border-slate-300 px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
            >
              <option value="">Todos os tipos</option>
              <option value="Despesa">Despesa</option>
              <option value="Receita">Receita</option>
            </select>
          </div>
        </div>

        {erroPessoas && <MensagemErro mensagem={erroPessoas} />}
        {carregando && <Carregando />}
        {erro && <MensagemErro mensagem={erro} />}
        {!carregando && !erro && transacoes && transacoes.length === 0 && (
          <MensagemVazia mensagem="Nenhuma transação encontrada." />
        )}
        {!carregando && !erro && transacoes && transacoes.length > 0 && (
          <ul className="divide-y divide-slate-200 rounded-md border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
            {transacoes.map((transacao) => (
              <li key={transacao.id} className="flex items-center justify-between px-4 py-2.5">
                <div className="flex flex-col">
                  <span className="font-medium">{transacao.descricao}</span>
                  <span className="text-sm text-slate-500 dark:text-slate-400">{transacao.pessoaNome}</span>
                </div>
                <div className="flex items-center gap-3">
                  <BadgeTipo tipo={transacao.tipo} />
                  <span
                    className={
                      transacao.tipo === 'Receita'
                        ? 'font-medium text-emerald-600 dark:text-emerald-400'
                        : 'font-medium text-red-600 dark:text-red-400'
                    }
                  >
                    {formatarMoeda(transacao.valor)}
                  </span>
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
