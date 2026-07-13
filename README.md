# OrçaLar

Sistema de controle de gastos residenciais: cadastro de pessoas, registro de suas
transações (receitas e despesas) e consulta de totais consolidados por pessoa e geral —
com lançamentos recorrentes (Fixas) e um dashboard que projeta o saldo futuro com base
neles.

**Deploy (Railway):** https://sistemadecontrolegastos-despesasfamiliares-production.up.railway.app

## Funcionalidades

- **Pessoas** — cadastro, listagem (com busca por nome) e remoção.
- **Transações** — registro de receitas e despesas associadas a uma pessoa, com data
  (opcional na criação — assume a data do servidor quando omitida), listagem ordenada por
  data (mais recente primeiro) e filtros (por pessoa e por tipo).
- **Totais** — consulta consolidada: receitas, despesas e saldo por pessoa, mais o total
  geral da casa.
- **Fixas** _(extra)_ — tela própria (`/fixas`) para cadastro de lançamentos recorrentes
  (ex.: aluguel, mesada), com dia do mês, vigência (início e, opcionalmente, fim). Não são
  transações: nunca aparecem em `/api/transacoes` nem em `/api/totais`, nem na listagem de
  Transações.
- **Dashboard** _(extra)_ — na página de Totais, abaixo da tabela oficial: série temporal
  (Recharts) combinando o histórico real com a projeção das Fixas — gráfico de saldo
  acumulado (sólido = realizado, pontilhado = projeção) e gráfico de receitas × despesas
  por período, com toggle mês/dia e horizonte de projeção configurável (3/6/12 meses).

## Stack

- **Back-end**: .NET 10 (ASP.NET Core Web API) + Entity Framework Core / Npgsql + PostgreSQL.
- **Front-end**: Vite + React + TypeScript + Tailwind CSS v4 + react-router + Recharts (`web/`).
- **Testes**: xUnit + EF Core InMemory (`tests/`).
- **Empacotamento**: uma única imagem Docker (o back-end serve a SPA buildada).

## Decisões de arquitetura (e o porquê)

- **Controllers → Services → EF Core**: os Controllers só orquestram (recebem o DTO,
  chamam o Service, traduzem o retorno em status HTTP); toda regra de negócio mora nos
  Services. Isso mantém a regra testável isoladamente (ver `tests/`), sem precisar subir
  um servidor HTTP para validar uma regra de domínio.
- **`Guid` como identificador**, gerado na aplicação: evita depender de uma sequência
  incremental do banco (que vaza a ordem/contagem de criação e não é trivial de manter
  consistente entre ambientes diferentes), e permite gerar o id antes mesmo de persistir.
- **`decimal` em C# / `numeric(18,2)` no Postgres** para dinheiro, nunca `float`/`double`:
  ponto flutuante binário não representa exatamente valores decimais (ex.: 0,1 + 0,2 ≠
  0,3), o que acumula erro de arredondamento em somas — inaceitável para valores monetários.
- **Enum `TipoTransacao` serializado como string** (`"Despesa"`/`"Receita"`) em vez do
  índice numérico: o índice depende da ordem de declaração no código — se alguém inverter
  ou inserir um valor no meio do enum no futuro, um índice antigo persistido no banco (ou
  em cache no cliente) passaria a significar outra coisa silenciosamente.
- **422 (regra de negócio) vs. 400 (formato)**: um 400 significa que a requisição em si
  está malformada (campo obrigatório faltando, idade fora do range, JSON inválido) — erro
  de forma. Um 422 significa que a requisição está bem formada, mas viola uma regra do
  domínio (menor de idade não pode ter receita; pessoa referenciada não existe) — erro de
  significado. Separar os dois códigos permite ao cliente da API distinguir "eu montei a
  requisição errada" de "essa operação não é permitida aqui".
- **Cascade na deleção de Pessoa**: uma Transacao sem Pessoa correspondente não tem
  sentido no domínio (o campo `pessoaId` é obrigatório e imutável) — por isso apagar a
  pessoa apaga suas transações junto, tanto via constraint do banco (`ON DELETE CASCADE`)
  quanto no tracking do EF Core (necessário para o mesmo comportamento valer também com o
  provider InMemory usado nos testes).
- **Same-origin / imagem única**: o back-end serve os estáticos da SPA buildada
  (`UseStaticFiles` + `MapFallbackToFile`), então front e back sempre respondem na mesma
  origem — elimina CORS como fonte de bug e reduz o deploy a um único artefato (uma
  imagem, um serviço), em vez de coordenar dois deploys e uma configuração de CORS entre eles.
- **Migrations aplicadas no boot, com retry**: evita um passo manual de "rodar migration"
  em cada deploy — a própria API garante que o schema está atualizado ao subir. O retry
  existe porque, tanto em `docker compose` quanto no Railway, o serviço do banco pode
  ainda não estar aceitando conexões no instante em que o container da API inicia; sem
  retry, essa corrida derrubaria o primeiro boot.

## Decisões dos extras (data, fixas e dashboard)

- **Fixa é uma REGRA, não uma transação, e nunca persiste ocorrências**: cadastrar uma
  Fixa não cria nenhuma linha em `Transacao`. Suas ocorrências são calculadas **sob
  demanda**, a cada requisição ao dashboard, pelo motor de projeção (`MotorProjecaoFixas`).
  Não existe scheduler, job em background nem tabela de "ocorrências geradas" — se a regra
  mudar (valor, dia, vigência), a projeção muda imediatamente na próxima consulta, sem
  precisar reprocessar nada retroativamente.
- **`/api/totais` e `/api/transacoes` continuam refletindo só transações reais**: os
  extras foram construídos ao lado do que já existia, sem alterar esses dois contratos —
  Fixas e a projeção vivem inteiramente em `/api/fixas` e `/api/dashboard/series`.
- **Separação Realizado vs. Projetado**: cada ponto da série do dashboard traz um booleano
  `projetado` (`false` = veio de transações reais; `true` = veio da expansão de Fixas). O
  front usa isso para renderizar duas séries sobre o mesmo `LineChart` (uma só preenchida
  nos pontos reais, outra só nos projetados, cada uma `null` no resto) — o último ponto
  real também recebe o valor na série projetada, criando o vértice que conecta visualmente
  o traço sólido (realizado) ao pontilhado (projeção) exatamente em "hoje".
- **Toggle mês/dia**: é o parâmetro `granularidade` do próprio endpoint (`mes` agrupa
  contínuo por mês; `dia` só emite dias com evento, para não gerar ~365 buckets vazios) —
  no front, isso é literalmente o estado que dispara um novo fetch, sem lógica adicional.
- **Data como fonte única do servidor**: igual à Fixa (`DataInicio` default), o campo
  `Data` de Transacao usa `DateOnly.FromDateTime(DateTime.UtcNow)` quando omitido — nunca
  se confia no relógio do cliente.

## Regras de negócio

- **Pessoa**: criação, listagem (busca parcial por nome, case-insensitive) e remoção. Ao
  remover uma pessoa, todas as suas transações são removidas em cascata.
- **Transação**: apenas criação e listagem (sem edição/remoção).
  - O `pessoaId` informado precisa existir; caso contrário, a criação é rejeitada (422,
    `PESSOA_NAO_ENCONTRADA`).
  - Pessoas menores de 18 anos só podem cadastrar **despesas** — uma tentativa de
    cadastrar **receita** para um menor é rejeitada (422, `REGRA_MENOR_RECEITA`). A partir
    de 18 anos (inclusive), ambos os tipos são permitidos.
- **Totais**: para cada pessoa, soma de receitas, soma de despesas e saldo (receitas −
  despesas). Pessoas sem nenhuma transação aparecem com 0/0/0 (não somem da lista). O
  total geral soma todas as pessoas.
- **Fixa**: mesmas duas regras de Transação (pessoa precisa existir → 422
  `PESSOA_NAO_ENCONTRADA`; menor de 18 anos não pode ter receita fixa → 422
  `REGRA_MENOR_RECEITA`), mais validação de formato (400): valor > 0, `diaDoMes` entre 1
  e 31, e `dataFim ≥ dataInicio` quando ambas informadas. Dias que não existem no mês
  (ex.: 31 em fevereiro) sofrem clamp para o último dia real do mês na hora de projetar.
  Apenas criação, listagem (filtro opcional por pessoa) e remoção — sem edição.
- **Dashboard**: `granularidade` (`mes` padrão ou `dia`) e `mesesProjecao` (padrão 6,
  quantos meses projetar a partir de hoje). Histórico = só transações reais, do primeiro
  registro até hoje. Projeção = ocorrências de Fixas, de hoje até hoje + `mesesProjecao`,
  com o saldo acumulado continuando exatamente de onde o real parou.

## Estrutura de pastas

```
orcalar/
  Dockerfile              build multi-stage (front → back → runtime), imagem única
  docker-compose.yml      sobe banco + app localmente com um comando
  railway.toml             aponta o builder do Railway pro Dockerfile (opcional)
  global.json              pin da versão do SDK .NET
  scripts/
    smoke-test.sh          smoke test HTTP contra uma instância real (dev ou dockerizada)

  src/OrcaLar.Api/
    Controllers/           endpoints HTTP (só orquestram)
    Services/               regras de negócio (Pessoa, Transacao, Fixa, Totais, Dashboard)
                             + motores puros (MotorProjecaoFixas, MotorSeries) e o helper
                             de regras compartilhadas (RegrasLancamento)
    Data/                   AppDbContext, configuração Fluent e migrations
    Domain/Entities/        entidades de domínio (Pessoa, Transacao, Fixa, TipoTransacao)
    Dtos/                   contratos de request/response
    Infrastructure/         parser de DATABASE_URL, retry de migration, erro/exceção
    Program.cs               pipeline: erro → static files → controllers → SPA fallback

  tests/OrcaLar.Api.Tests/
    Services/               testes das regras de negócio (EF Core InMemory)

  web/src/
    api/                     client HTTP central (envelope de erro) + uma função por endpoint
                             (pessoas, transacoes, fixas, dashboard)
    types/                   tipos TS espelhando os DTOs reais do back
    hooks/                   useApiData (loading/erro) e useDebounce (busca por nome)
    utils/                   formatarMoeda (R$) e formatarData/formatarPeriodo (datas)
    components/              Layout (nav), estados de loading/erro/vazio, badge de tipo,
                             Dashboard (gráficos Recharts da página de Totais)
    pages/                   as 4 telas: Pessoas, Transações, Fixas, Totais (com dashboard)
    router.tsx               as 4 rotas + redirect da raiz
```

## Contrato da API (base `/api`)

| Método | Rota                              | Descrição                                   |
|--------|------------------------------------|----------------------------------------------|
| POST   | `/api/pessoas`                    | Cria uma pessoa                               |
| GET    | `/api/pessoas?nome=`               | Lista pessoas (filtro opcional por nome)      |
| DELETE | `/api/pessoas/{id}`                | Remove uma pessoa (e suas transações)         |
| POST   | `/api/transacoes`                 | Cria uma transação (data opcional)            |
| GET    | `/api/transacoes?pessoaId=&tipo=` | Lista transações (filtros opcionais), por data desc |
| GET    | `/api/totais`                     | Totais por pessoa + total geral               |
| POST   | `/api/fixas`                       | Cria uma regra de lançamento recorrente       |
| GET    | `/api/fixas?pessoaId=`            | Lista fixas (filtro opcional por pessoa)      |
| DELETE | `/api/fixas/{id}`                  | Remove uma fixa                               |
| GET    | `/api/dashboard/series?granularidade=&mesesProjecao=` | Série real + projetada |

Erros seguem sempre o mesmo envelope:

```json
{ "error": { "code": "REGRA_MENOR_RECEITA", "message": "Pessoas menores de 18 anos não podem cadastrar receitas." } }
```

- `400`: payload malformado ou inválido (código `DADOS_INVALIDOS`).
- `422`: violação de regra de negócio (`REGRA_MENOR_RECEITA` ou `PESSOA_NAO_ENCONTRADA`).

## Como rodar

### a) Produção local — um comando (Docker)

Requer apenas Docker e Docker Compose instalados.

```bash
docker compose up --build
```

Isso sobe o Postgres, aguarda ele ficar saudável (`healthcheck` + `depends_on`), builda a
imagem única (front + back) e inicia a aplicação — que aplica as migrations automaticamente
(com retry) e serve a API e a SPA na mesma origem, em `http://localhost:8080`.

Credenciais do banco (só para uso local — nunca reais) podem ser customizadas via `.env`
(veja `.env.example`); sem um `.env`, os defaults (`orcalar`/`orcalar`/`orcalar`) valem.

### b) Desenvolvimento — back e front separados

Útil para hot-reload do front e depuração do back de forma independente.

**1. Banco** (via Docker, reaproveitando o compose, ou qualquer Postgres acessível):

```bash
docker compose up -d db
```

**2. Back-end:**

```bash
cp .env.example .env   # ajuste se necessário
export DATABASE_URL="postgresql://orcalar:orcalar@localhost:5432/orcalar"
export PORT=8080
dotnet run --project src/OrcaLar.Api
```

As migrations são aplicadas automaticamente na subida. A API escuta em `http://0.0.0.0:8080`.

**3. Front-end** (em outro terminal):

```bash
cd web
npm install
npm run dev
```

Abre em `http://localhost:5173`. O proxy configurado em `web/vite.config.ts` encaminha
`/api/...` para `http://localhost:8080` — por isso não há CORS em dev, e em produção
(imagem única) o próprio back serve os estáticos, mantendo o same-origin real.

### c) Testes

```bash
dotnet test
```

Usam EF Core InMemory — não dependem de nenhum Postgres rodando.

### d) Smoke test HTTP (opcional)

Com a API rodando (via `dotnet run` ou dockerizada, na porta que estiver publicada):

```bash
./scripts/smoke-test.sh
# ou, se a API estiver em outra porta/host:
BASE_URL=http://localhost:8080 ./scripts/smoke-test.sh
```

Exercita o caminho feliz e as regras de negócio (menor não pode ter receita — inclusive
via Fixa —, pessoa inexistente é rejeitada, cascade na remoção de transações e fixas,
série do dashboard nas duas granularidades etc.) via HTTP real — complementa os testes
automatizados, que rodam isolados via InMemory. Requer `curl` e `jq`.

## Deploy no Railway

O Railway builda direto pelo `Dockerfile` da raiz (autodetectado, ou explicitado em
`railway.toml`). Passos:

1. Crie um novo projeto no Railway e adicione um serviço a partir deste repositório
   (Railway detecta o `Dockerfile` automaticamente).
2. Adicione um serviço de banco **PostgreSQL** (plugin nativo do Railway) ao mesmo projeto.
3. No serviço da aplicação, defina a variável `DATABASE_URL` referenciando a do serviço
   Postgres (o Railway já a expõe nesse formato URI, com SSL — o mesmo
   `DatabaseUrlParser` usado localmente já trata isso, sem nenhuma configuração extra).
4. Não defina `PORT` manualmente — o Railway injeta a própria porta automaticamente, e a
   aplicação já lê essa variável (`Program.cs`).
5. Nenhuma configuração de CORS é necessária: front e back são servidos pela mesma
   origem (a imagem única), e nada no código depende de `localhost` fixo.

Depois do deploy, cole a URL pública no topo deste README.

## Notas de UX (front-end)

- A regra "menor de 18 anos só pode ter despesa" é refletida nos formulários de criação de
  transação **e** de fixa (a opção "Receita" fica desabilitada) — isso é só prevenção de
  UX; quem decide de fato é o back-end, que responde 422 (`REGRA_MENOR_RECEITA`) se essa
  checagem for burlada.
- Toda resposta de erro do back (400/422) é lida do envelope `{ error: { code, message } }`
  e a `message` é exibida diretamente ao usuário.
- O campo de data (transação e início de fixa) mostra "vazio = hoje": omitir o campo não é
  um erro, é o caminho normal para "agora".
- A tela de Fixas deixa explícito, em um subtítulo, que fixa é uma regra de recorrência —
  não uma transação — para não confundir com a listagem de Transações.
