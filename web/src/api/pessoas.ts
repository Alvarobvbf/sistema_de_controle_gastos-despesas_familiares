import { apiFetch } from './client'
import type { CriarPessoaRequest, Pessoa } from '../types/api'

export function listarPessoas(nome?: string): Promise<Pessoa[]> {
  return apiFetch<Pessoa[]>('/pessoas', { query: { nome } })
}

export function criarPessoa(dados: CriarPessoaRequest): Promise<Pessoa> {
  return apiFetch<Pessoa>('/pessoas', { method: 'POST', body: dados })
}

export function deletarPessoa(id: string): Promise<void> {
  return apiFetch<void>(`/pessoas/${id}`, { method: 'DELETE' })
}
