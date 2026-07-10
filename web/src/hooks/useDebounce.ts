import { useEffect, useState } from 'react'

/** Atrasa a propagação de um valor — evita disparar uma requisição a cada tecla digitada. */
export function useDebounce<T>(valor: T, atrasoMs = 300): T {
  const [valorAtrasado, setValorAtrasado] = useState(valor)

  useEffect(() => {
    const timer = setTimeout(() => setValorAtrasado(valor), atrasoMs)
    return () => clearTimeout(timer)
  }, [valor, atrasoMs])

  return valorAtrasado
}
