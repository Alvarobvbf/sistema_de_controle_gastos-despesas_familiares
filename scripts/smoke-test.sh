#!/usr/bin/env bash
#
# Smoke test do back-end OrçaLar contra uma instância já rodando (dotnet run).
# Não substitui os testes automatizados (dotnet test) — esses cobrem as regras de negócio
# isoladas via EF Core InMemory. Este script bate na API de verdade (HTTP real), útil para
# conferir rapidamente, depois de qualquer mudança, que o caminho feliz e as regras de
# negócio continuam respondendo com os status/códigos de erro corretos.
#
# Uso:
#   ./scripts/smoke-test.sh
#   BASE_URL=http://localhost:9000 ./scripts/smoke-test.sh   # se a API estiver em outra porta
#
# Requer: curl, jq. Sai com código 0 se tudo passar, 1 se algo falhar.

set -uo pipefail

BASE_URL="${BASE_URL:-http://localhost:8080}"
TOTAL=0
FALHAS=0

verificar() {
  local descricao="$1" esperado="$2" obtido="$3"
  TOTAL=$((TOTAL + 1))
  if [[ "$obtido" == "$esperado" ]]; then
    echo "  OK     $descricao (HTTP $obtido)"
  else
    echo "  FALHOU $descricao — esperado HTTP $esperado, obtido HTTP $obtido"
    echo "         corpo: $(cat /tmp/orcalar-smoke-resp.json 2>/dev/null)"
    FALHAS=$((FALHAS + 1))
  fi
}

# Faz a chamada, salva o corpo em /tmp/orcalar-smoke-resp.json e devolve o status HTTP.
chamar() {
  local metodo="$1" caminho="$2" corpo="${3:-}"
  if [[ -n "$corpo" ]]; then
    curl -s -o /tmp/orcalar-smoke-resp.json -w "%{http_code}" -X "$metodo" "$BASE_URL$caminho" \
      -H "Content-Type: application/json" -d "$corpo"
  else
    curl -s -o /tmp/orcalar-smoke-resp.json -w "%{http_code}" -X "$metodo" "$BASE_URL$caminho"
  fi
}

corpo() { cat /tmp/orcalar-smoke-resp.json; }

echo "== OrçaLar smoke test — $BASE_URL =="

echo
echo "-- Pessoas: criação e validação --"

status=$(chamar POST /api/pessoas '{"nome":"Ana Adulta","idade":30}')
verificar "criar pessoa adulta" 201 "$status"
ANA_ID=$(corpo | jq -r '.id')

status=$(chamar POST /api/pessoas '{"nome":"Bia Menor","idade":15}')
verificar "criar pessoa menor de idade" 201 "$status"
BIA_ID=$(corpo | jq -r '.id')

status=$(chamar POST /api/pessoas '{"nome":"","idade":30}')
verificar "rejeitar nome vazio" 400 "$status"

status=$(chamar POST /api/pessoas '{"nome":"Idade Inválida","idade":200}')
verificar "rejeitar idade fora do range (0-150)" 400 "$status"

echo
echo "-- Transações: regras de negócio --"

status=$(chamar POST /api/transacoes "{\"descricao\":\"Lanche\",\"valor\":20,\"tipo\":\"Despesa\",\"pessoaId\":\"$BIA_ID\"}")
verificar "despesa para menor de idade: permitido" 201 "$status"

status=$(chamar POST /api/transacoes "{\"descricao\":\"Mesada\",\"valor\":50,\"tipo\":\"Receita\",\"pessoaId\":\"$BIA_ID\"}")
verificar "receita para menor de idade: rejeitado (422)" 422 "$status"
codigo=$(corpo | jq -r '.error.code')
[[ "$codigo" == "REGRA_MENOR_RECEITA" ]] \
  && echo "  OK     código de erro = REGRA_MENOR_RECEITA" \
  || { echo "  FALHOU código de erro inesperado: $codigo"; FALHAS=$((FALHAS + 1)); }

status=$(chamar POST /api/transacoes "{\"descricao\":\"Salário\",\"valor\":5000,\"tipo\":\"Receita\",\"pessoaId\":\"$ANA_ID\"}")
verificar "receita para adulto: permitido" 201 "$status"

status=$(chamar POST /api/transacoes "{\"descricao\":\"Aluguel\",\"valor\":1500,\"tipo\":\"Despesa\",\"pessoaId\":\"$ANA_ID\"}")
verificar "despesa para adulto: permitido" 201 "$status"

status=$(chamar POST /api/transacoes '{"descricao":"Fantasma","valor":10,"tipo":"Despesa","pessoaId":"00000000-0000-0000-0000-000000000000"}')
verificar "transação para pessoa inexistente: rejeitado (422)" 422 "$status"
codigo=$(corpo | jq -r '.error.code')
[[ "$codigo" == "PESSOA_NAO_ENCONTRADA" ]] \
  && echo "  OK     código de erro = PESSOA_NAO_ENCONTRADA" \
  || { echo "  FALHOU código de erro inesperado: $codigo"; FALHAS=$((FALHAS + 1)); }

echo
echo "-- Totais --"

status=$(chamar GET /api/totais)
verificar "consultar totais" 200 "$status"
# Comparação numérica (via jq), não textual: o valor vem como "3500.00" (numeric(18,2)),
# então comparar strings ("3500" != "3500.00") daria falso negativo mesmo estando correto.
saldo_ana_ok=$(corpo | jq -r --arg id "$ANA_ID" '(.pessoas[] | select(.pessoaId == $id) | .saldo) == 3500')
if [[ "$saldo_ana_ok" == "true" ]]; then
  echo "  OK     saldo da Ana = 3500 (5000 receita − 1500 despesa)"
else
  saldo_ana=$(corpo | jq -r --arg id "$ANA_ID" '.pessoas[] | select(.pessoaId == $id) | .saldo')
  echo "  FALHOU saldo da Ana inesperado: $saldo_ana"
  FALHAS=$((FALHAS + 1))
fi

echo
echo "-- Deleção em cascata --"

status=$(chamar DELETE "/api/pessoas/$ANA_ID")
verificar "deletar pessoa" 204 "$status"

status=$(chamar GET "/api/transacoes?pessoaId=$ANA_ID")
verificar "listar transações da pessoa deletada" 200 "$status"
qtde=$(corpo | jq -r 'length')
[[ "$qtde" == "0" ]] \
  && echo "  OK     transações da pessoa deletada foram removidas em cascata" \
  || { echo "  FALHOU ainda existem $qtde transações da pessoa deletada"; FALHAS=$((FALHAS + 1)); }

status=$(chamar DELETE "/api/pessoas/$ANA_ID")
verificar "deletar pessoa já deletada: 404" 404 "$status"

echo
echo "== Resultado: $((TOTAL - FALHAS))/$TOTAL passaram =="

rm -f /tmp/orcalar-smoke-resp.json

if [[ "$FALHAS" -gt 0 ]]; then
  exit 1
fi
