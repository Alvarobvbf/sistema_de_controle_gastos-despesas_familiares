import { apiFetch } from './client'
import type { CriarFixaRequest, Fixa } from '../types/api'

export function listarFixas(pessoaId?: string): Promise<Fixa[]> {
  return apiFetch<Fixa[]>('/fixas', { query: { pessoaId } })
}

export function criarFixa(dados: CriarFixaRequest): Promise<Fixa> {
  return apiFetch<Fixa>('/fixas', { method: 'POST', body: dados })
}

export function deletarFixa(id: string): Promise<void> {
  return apiFetch<void>(`/fixas/${id}`, { method: 'DELETE' })
}
