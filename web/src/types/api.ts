// Tipos espelhando exatamente os DTOs do back-end (src/OrcaLar.Api/Dtos), incluindo os
// nomes de campo em camelCase (política padrão de serialização JSON do ASP.NET Core).

/** Espelha o enum TipoTransacao, serializado como string pelo back (JsonStringEnumConverter). */
export type TipoTransacao = 'Despesa' | 'Receita'

export interface Pessoa {
  id: string
  nome: string
  idade: number
}

export interface CriarPessoaRequest {
  nome: string
  idade: number
}

export interface Transacao {
  id: string
  descricao: string
  valor: number
  tipo: TipoTransacao
  pessoaId: string
  pessoaNome: string
  /** DateOnly do back, serializado como "yyyy-MM-dd" — mesmo formato do <input type="date">. */
  data: string
}

export interface CriarTransacaoRequest {
  descricao: string
  valor: number
  tipo: TipoTransacao
  pessoaId: string
  /** Omitida (undefined) quando o usuário não preenche — o back assume a data de hoje. */
  data?: string
}

export interface Fixa {
  id: string
  descricao: string
  valor: number
  tipo: TipoTransacao
  pessoaId: string
  pessoaNome: string
  diaDoMes: number
  dataInicio: string
  dataFim: string | null
}

export interface CriarFixaRequest {
  descricao: string
  valor: number
  tipo: TipoTransacao
  pessoaId: string
  diaDoMes: number
  /** Omitida quando o usuário não preenche — o back assume a data de hoje. */
  dataInicio?: string
  dataFim?: string
}

/** Espelha o enum Granularidade do back, serializado como string. */
export type Granularidade = 'Mes' | 'Dia'

export interface SeriePonto {
  periodo: string
  receitas: number
  despesas: number
  saldoPeriodo: number
  saldoAcumulado: number
  projetado: boolean
}

export interface SerieResponse {
  granularidade: Granularidade
  pontos: SeriePonto[]
}

export interface PessoaTotal {
  pessoaId: string
  nome: string
  totalReceitas: number
  totalDespesas: number
  saldo: number
}

export interface TotalGeral {
  totalReceitas: number
  totalDespesas: number
  saldoLiquido: number
}

export interface TotaisResponse {
  pessoas: PessoaTotal[]
  totalGeral: TotalGeral
}

/** Envelope de erro padrão da API (400 e 422): { error: { code, message } }. */
export interface ErroApi {
  error: {
    code: string
    message: string
  }
}
