import { apiFetch } from './client'
import type { Granularidade, SerieResponse } from '../types/api'

export interface FiltrosSeries {
  granularidade: Granularidade
  mesesProjecao: number
}

export function obterSeries(filtros: FiltrosSeries): Promise<SerieResponse> {
  // O binder de enum do ASP.NET Core aceita o nome em qualquer caixa — enviar minúsculo
  // aqui é só estética da URL, o back devolve o valor sempre em PascalCase ("Mes"/"Dia").
  return apiFetch<SerieResponse>('/dashboard/series', {
    query: {
      granularidade: filtros.granularidade.toLowerCase(),
      mesesProjecao: String(filtros.mesesProjecao),
    },
  })
}
