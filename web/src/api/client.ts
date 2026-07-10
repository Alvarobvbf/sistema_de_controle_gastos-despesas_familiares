import type { ErroApi } from '../types/api'

/**
 * Erro de negócio/validação devolvido pelo back no envelope { error: { code, message } }
 * (400 ou 422). `code` existe para tratamento programático (ex.: destacar um campo
 * específico), mas `message` já vem pronta em PT-BR para mostrar ao usuário — o back é
 * a autoridade da regra, o front só repassa o texto.
 */
export class ApiError extends Error {
  readonly code: string
  readonly status: number

  constructor(code: string, message: string, status: number) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.status = status
  }
}

/** Erro de rede/conexão — não veio do back, então não existe envelope nem código de negócio. */
export class ErroDeRede extends Error {
  constructor() {
    super('Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.')
    this.name = 'ErroDeRede'
  }
}

const BASE_URL = '/api'

interface OpcoesRequisicao {
  method?: 'GET' | 'POST' | 'DELETE'
  body?: unknown
  /** Parâmetros de query; entradas undefined ou vazias são omitidas da URL. */
  query?: Record<string, string | undefined>
}

function montarUrl(caminho: string, query?: Record<string, string | undefined>): string {
  const url = new URL(`${BASE_URL}${caminho}`, window.location.origin)
  if (query) {
    for (const [chave, valor] of Object.entries(query)) {
      if (valor !== undefined && valor !== '') {
        url.searchParams.set(chave, valor)
      }
    }
  }
  // Só o caminho relativo é usado de fato — window.location.origin é só um "âncora"
  // exigido pela API URL para poder montar query params; a app sempre chama /api/...
  // (same-origin), nunca uma URL absoluta.
  return `${url.pathname}${url.search}`
}

/**
 * Wrapper fino sobre fetch: monta a URL relativa (/api/...), serializa o corpo e trata a
 * resposta de forma centralizada. Toda resposta não-ok tem seu envelope de erro lido e
 * vira um ApiError com a message pronta — as telas nunca precisam parsear o erro na mão.
 */
export async function apiFetch<T>(caminho: string, opcoes: OpcoesRequisicao = {}): Promise<T> {
  const { method = 'GET', body, query } = opcoes
  const url = montarUrl(caminho, query)

  let resposta: Response
  try {
    resposta = await fetch(url, {
      method,
      headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new ErroDeRede()
  }

  // DELETE bem-sucedido devolve 204 sem corpo.
  if (resposta.status === 204) {
    return undefined as T
  }

  if (!resposta.ok) {
    // Nem todo erro vem no envelope: DELETE de pessoa inexistente devolve 404 puro
    // (NotFound() do ASP.NET Core), sem { error: ... }. Por isso o parse é tolerante.
    let corpoErro: ErroApi | null = null
    try {
      corpoErro = await resposta.json()
    } catch {
      corpoErro = null
    }

    if (corpoErro?.error) {
      throw new ApiError(corpoErro.error.code, corpoErro.error.message, resposta.status)
    }

    throw new ApiError('ERRO_HTTP', `O servidor respondeu com um erro inesperado (HTTP ${resposta.status}).`, resposta.status)
  }

  return (await resposta.json()) as T
}
