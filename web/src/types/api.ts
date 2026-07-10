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
}

export interface CriarTransacaoRequest {
  descricao: string
  valor: number
  tipo: TipoTransacao
  pessoaId: string
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
