import { apiFetch } from './client'
import type { TotaisResponse } from '../types/api'

export function obterTotais(): Promise<TotaisResponse> {
  return apiFetch<TotaisResponse>('/totais')
}
