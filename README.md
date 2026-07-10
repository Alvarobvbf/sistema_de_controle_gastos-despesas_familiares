# OrçaLar

Sistema de controle de gastos residenciais: cadastro de pessoas e de suas transações
(receitas e despesas), com consulta de totais consolidados.

> Status atual: back-end e front-end implementados e rodando localmente (front contra o
> back via proxy do Vite). O Docker Compose fica reservado para a próxima rodada.

## Stack

- .NET 10 (ASP.NET Core Web API)
- Entity Framework Core + Npgsql (PostgreSQL)
- xUnit + EF Core InMemory (testes)
- Vite + React + TypeScript + Tailwind CSS v4 + react-router (`web/`)

## Arquitetura

Controllers → Services (toda a regra de negócio mora aqui) → EF Core (`AppDbContext`) → PostgreSQL.

```
src/OrcaLar.Api/
  Controllers/     endpoints HTTP (só orquestram: recebem DTO, chamam Service, retornam status)
  Services/        regras de negócio (Pessoa, Transacao, Totais)
  Data/            AppDbContext, configuração Fluent e migrations
  Domain/Entities/ entidades de domínio (Pessoa, Transacao, TipoTransacao)
  Dtos/            contratos de request/response
  Infrastructure/  parser de DATABASE_URL, retry de migration, exceção e middleware de erro
tests/OrcaLar.Api.Tests/
  Services/        testes das regras de negócio (EF Core InMemory)
```

## Regras de negócio

- **Pessoa**: criação, listagem (com filtro opcional por nome, busca parcial e
  case-insensitive) e deleção. Ao deletar uma pessoa, todas as suas transações são
  removidas em cascata.
- **Transação**: apenas criação e listagem (sem edição/deleção).
  - O `pessoaId` informado precisa existir; caso contrário, a criação é rejeitada (422).
  - Pessoas menores de 18 anos só podem cadastrar **despesas** — uma tentativa de
    cadastrar **receita** para um menor é rejeitada (422). A partir de 18 anos (inclusive),
    ambos os tipos são permitidos.
- **Totais**: para cada pessoa cadastrada, soma de receitas, soma de despesas e saldo
  (receitas − despesas). Pessoas sem nenhuma transação aparecem com 0/0/0 (não somem da
  lista). Ao final, o total geral soma todas as pessoas.

## Contrato da API (base `/api`)

| Método | Rota                              | Descrição                                   |
|--------|------------------------------------|----------------------------------------------|
| POST   | `/api/pessoas`                    | Cria uma pessoa                               |
| GET    | `/api/pessoas?nome=`               | Lista pessoas (filtro opcional por nome)      |
| DELETE | `/api/pessoas/{id}`                | Remove uma pessoa (e suas transações)         |
| POST   | `/api/transacoes`                 | Cria uma transação                            |
| GET    | `/api/transacoes?pessoaId=&tipo=` | Lista transações (filtros opcionais)          |
| GET    | `/api/totais`                     | Totais por pessoa + total geral               |

Erros seguem sempre o mesmo envelope:

```json
{ "error": { "code": "REGRA_MENOR_RECEITA", "message": "Pessoas menores de 18 anos não podem cadastrar receitas." } }
```

- `400`: payload malformado ou inválido (código `DADOS_INVALIDOS`).
- `422`: violação de regra de negócio (`REGRA_MENOR_RECEITA` ou `PESSOA_NAO_ENCONTRADA`).

## Como rodar localmente

### 1. Subir um PostgreSQL

Qualquer Postgres acessível serve. Exemplo rápido com Docker:

```bash
docker run -d --name orcalar-db \
  -e POSTGRES_USER=orcalar \
  -e POSTGRES_PASSWORD=orcalar \
  -e POSTGRES_DB=orcalar \
  -p 5432:5432 \
  postgres:17-alpine
```

### 2. Configurar a `DATABASE_URL`

Copie `.env.example` para `.env` e ajuste se necessário (o valor padrão já aponta para o
Postgres do passo anterior). A API lê a variável de ambiente diretamente — exporte-a na
sessão do terminal antes de rodar:

```bash
export DATABASE_URL="postgresql://orcalar:orcalar@localhost:5432/orcalar"
export PORT=8080
```

### 3. Rodar a API

```bash
dotnet run --project src/OrcaLar.Api
```

As migrations são aplicadas automaticamente na subida (com retry, caso o Postgres ainda
não esteja pronto). A API escuta em `http://0.0.0.0:8080` (ou na porta definida em `PORT`).

### 4. Rodar os testes

```bash
dotnet test
```

Os testes usam EF Core InMemory e não dependem do Postgres subido no passo 1.

### 5. Smoke test contra a API real (opcional)

Com a API do passo 3 rodando, em outro terminal:

```bash
./scripts/smoke-test.sh
```

Exercita o caminho feliz e as regras de negócio (menor não pode ter receita, pessoa
inexistente é rejeitada, cascade na deleção etc.) via HTTP de verdade — complementa os
testes automatizados, que rodam isolados via InMemory. Requer `curl` e `jq`.

## Front-end (`web/`)

SPA em Vite + React + TypeScript + Tailwind v4, consumindo a API via URLs relativas
(`/api/...`, same-origin — sem CORS, sem base URL configurável).

```
web/src/
  api/        client HTTP central (envelope de erro) + uma função por endpoint
  types/      tipos TS espelhando os DTOs reais do back
  hooks/      useApiData (loading/erro) e useDebounce (busca por nome)
  components/ Layout (nav), estados de loading/erro/vazio, badge de tipo
  pages/      as 3 telas: Pessoas, Transações, Totais
  router.tsx  as 3 rotas + redirect da raiz
```

### Como rodar

Com o back-end já rodando localmente (seção anterior):

```bash
cd web
npm install
npm run dev
```

Abre em `http://localhost:5173`. O proxy configurado em `vite.config.ts` encaminha
`/api/...` para `http://localhost:8080` (mesma porta usada pelo back-end local) — por
isso não há CORS em dev, e em produção (imagem única, próxima rodada) o próprio back
serve os estáticos, mantendo o same-origin real.

### Notas de UX

- A regra "menor de 18 anos só pode ter despesa" é refletida no formulário de criação de
  transação (a opção "Receita" fica desabilitada) — isso é só prevenção de UX; quem decide
  de fato é o back-end, que responde 422 (`REGRA_MENOR_RECEITA`) se essa checagem for burlada.
- Toda resposta de erro do back (400/422) é lida do envelope `{ error: { code, message } }`
  e a `message` é exibida diretamente ao usuário.
