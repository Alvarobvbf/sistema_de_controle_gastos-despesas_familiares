import { useState, type FormEvent } from 'react'
import { useApiData } from '../hooks/useApiData'
import { useDebounce } from '../hooks/useDebounce'
import { listarPessoas, criarPessoa, deletarPessoa } from '../api/pessoas'
import { ApiError } from '../api/client'
import { Carregando, MensagemErro, MensagemVazia } from '../components/Estado'

const IDADE_MINIMA = 0
const IDADE_MAXIMA = 150

export function PessoasPage() {
  const [nomeFiltro, setNomeFiltro] = useState('')
  const nomeFiltroDebounced = useDebounce(nomeFiltro)
  const {
    dados: pessoas,
    carregando,
    erro,
    recarregar,
  } = useApiData(() => listarPessoas(nomeFiltroDebounced || undefined), [nomeFiltroDebounced])

  const [nome, setNome] = useState('')
  const [idade, setIdade] = useState('')
  const [erroFormulario, setErroFormulario] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  async function aoSubmeter(evento: FormEvent) {
    evento.preventDefault()
    setErroFormulario(null)

    const nomeAparado = nome.trim()
    const idadeNumero = Number(idade)

    // Validação de formato replicada aqui só por UX (feedback imediato, sem round-trip
    // ao back). O back (DataAnnotations) continua sendo a autoridade — o catch abaixo
    // trata qualquer 400 que escape desta checagem local.
    if (!nomeAparado) {
      setErroFormulario('Informe o nome.')
      return
    }
    if (
      idade.trim() === '' ||
      !Number.isInteger(idadeNumero) ||
      idadeNumero < IDADE_MINIMA ||
      idadeNumero > IDADE_MAXIMA
    ) {
      setErroFormulario(`Informe uma idade inteira entre ${IDADE_MINIMA} e ${IDADE_MAXIMA}.`)
      return
    }

    setEnviando(true)
    try {
      await criarPessoa({ nome: nomeAparado, idade: idadeNumero })
      setNome('')
      setIdade('')
      recarregar()
    } catch (erroCapturado) {
      setErroFormulario(
        erroCapturado instanceof ApiError ? erroCapturado.message : 'Não foi possível criar a pessoa.',
      )
    } finally {
      setEnviando(false)
    }
  }

  async function aoDeletar(id: string, nomePessoa: string) {
    // Aviso explícito do cascade: quem confirma precisa saber que as transações vão junto.
    const confirmado = window.confirm(
      `Tem certeza que deseja excluir "${nomePessoa}"? Todas as transações dessa pessoa serão apagadas junto.`,
    )
    if (!confirmado) return

    try {
      await deletarPessoa(id)
      recarregar()
    } catch (erroCapturado) {
      window.alert(erroCapturado instanceof ApiError ? erroCapturado.message : 'Não foi possível excluir a pessoa.')
    }
  }

  return (
    <div className="flex flex-col gap-8">
      <section>
        <h2 className="mb-3 text-xl font-semibold">Nova pessoa</h2>
        <form onSubmit={aoSubmeter} className="flex flex-wrap items-end gap-3">
          <div className="flex flex-col gap-1">
            <label htmlFor="nome" className="text-sm font-medium">
              Nome
            </label>
            <input
              id="nome"
              value={nome}
              onChange={(evento) => setNome(evento.target.value)}
              className="rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <div className="flex flex-col gap-1">
            <label htmlFor="idade" className="text-sm font-medium">
              Idade
            </label>
            <input
              id="idade"
              type="number"
              min={IDADE_MINIMA}
              max={IDADE_MAXIMA}
              step={1}
              value={idade}
              onChange={(evento) => setIdade(evento.target.value)}
              className="w-24 rounded-md border border-slate-300 px-3 py-1.5 dark:border-slate-700 dark:bg-slate-800"
            />
          </div>
          <button
            type="submit"
            disabled={enviando}
            className="rounded-md bg-slate-900 px-4 py-1.5 font-medium text-white disabled:opacity-50 dark:bg-slate-100 dark:text-slate-900"
          >
            {enviando ? 'Salvando...' : 'Adicionar'}
          </button>
        </form>
        {erroFormulario && (
          <div className="mt-3">
            <MensagemErro mensagem={erroFormulario} />
          </div>
        )}
      </section>

      <section>
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
          <h2 className="text-xl font-semibold">Pessoas cadastradas</h2>
          <input
            placeholder="Buscar por nome..."
            value={nomeFiltro}
            onChange={(evento) => setNomeFiltro(evento.target.value)}
            className="rounded-md border border-slate-300 px-3 py-1.5 text-sm dark:border-slate-700 dark:bg-slate-800"
          />
        </div>

        {carregando && <Carregando />}
        {erro && <MensagemErro mensagem={erro} />}
        {!carregando && !erro && pessoas && pessoas.length === 0 && (
          <MensagemVazia
            mensagem={
              nomeFiltro ? 'Nenhuma pessoa encontrada com esse nome.' : 'Nenhuma pessoa cadastrada ainda.'
            }
          />
        )}
        {!carregando && !erro && pessoas && pessoas.length > 0 && (
          <ul className="divide-y divide-slate-200 rounded-md border border-slate-200 dark:divide-slate-800 dark:border-slate-800">
            {pessoas.map((pessoa) => (
              <li key={pessoa.id} className="flex items-center justify-between px-4 py-2.5">
                <span>
                  {pessoa.nome}{' '}
                  <span className="text-slate-500 dark:text-slate-400">— {pessoa.idade} anos</span>
                </span>
                <button
                  type="button"
                  onClick={() => aoDeletar(pessoa.id, pessoa.nome)}
                  className="text-sm font-medium text-red-600 hover:underline dark:text-red-400"
                >
                  Excluir
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
