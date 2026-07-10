import { apiFetch } from './client'
import type { CriarTransacaoRequest, Transacao, TipoTransacao } from '../types/api'

export interface FiltrosTransacoes {
  pessoaId?: string
  tipo?: TipoTransacao
}

export function listarTransacoes(filtros: FiltrosTransacoes = {}): Promise<Transacao[]> {
  return apiFetch<Transacao[]>('/transacoes', {
    query: { pessoaId: filtros.pessoaId, tipo: filtros.tipo },
  })
}

export function criarTransacao(dados: CriarTransacaoRequest): Promise<Transacao> {
  return apiFetch<Transacao>('/transacoes', { method: 'POST', body: dados })
}
