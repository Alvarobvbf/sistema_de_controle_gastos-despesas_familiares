import { useCallback, useEffect, useState } from 'react'

interface EstadoApiData<T> {
  dados: T | null
  carregando: boolean
  erro: string | null
  /** Refaz a busca sem esperar uma mudança em `gatilho` (ex.: depois de criar um registro). */
  recarregar: () => void
}

/**
 * Hook genérico para chamadas de leitura (GET): controla loading/erro e refaz a busca
 * sempre que algo em `gatilho` mudar (ex.: filtros da tela) ou quando `recarregar` é
 * chamado manualmente. "Vazio" não é tratado aqui como estado à parte — cada tela decide
 * o que "vazio" significa a partir de `dados` (normalmente, array de tamanho 0).
 *
 * `buscar` é deliberadamente omitido do array de dependências do efeito: como ele é uma
 * closure recriada a cada render do componente chamador, incluí-lo faria o efeito rodar
 * a cada render, e não só quando `gatilho` de fato muda.
 */
export function useApiData<T>(buscar: () => Promise<T>, gatilho: unknown[]): EstadoApiData<T> {
  const [dados, setDados] = useState<T | null>(null)
  const [carregando, setCarregando] = useState(true)
  const [erro, setErro] = useState<string | null>(null)
  const [versao, setVersao] = useState(0)

  const recarregar = useCallback(() => setVersao((v) => v + 1), [])

  useEffect(() => {
    let cancelado = false
    setCarregando(true)
    setErro(null)

    buscar()
      .then((resultado) => {
        if (!cancelado) setDados(resultado)
      })
      .catch((erroCapturado: unknown) => {
        if (!cancelado) {
          setErro(erroCapturado instanceof Error ? erroCapturado.message : 'Erro inesperado.')
        }
      })
      .finally(() => {
        if (!cancelado) setCarregando(false)
      })

    return () => {
      cancelado = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...gatilho, versao])

  return { dados, carregando, erro, recarregar }
}
