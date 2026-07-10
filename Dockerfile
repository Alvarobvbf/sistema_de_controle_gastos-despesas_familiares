# syntax=docker/dockerfile:1

# Imagem única: o back-end ASP.NET Core serve a SPA buildada como estáticos (ver
# Program.cs — UseStaticFiles + MapFallbackToFile). Por isso o build do front acontece
# aqui dentro, e o resultado (web/dist) é copiado para wwwroot no stage final.

# ---- Stage 1: build do front (Vite + React + TS) -----------------------------------
FROM node:24 AS build-front
WORKDIR /web
# Copiar só os manifests primeiro aproveita o cache de camadas do Docker: o "npm ci"
# só roda de novo se package*.json mudar, não a cada alteração de código-fonte.
COPY web/package.json web/package-lock.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

# ---- Stage 2: build do back (ASP.NET Core) ------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-back
WORKDIR /source
# global.json fixa a versão do SDK também dentro do container, mesma lógica do dev local.
COPY global.json ./
# Mesmo raciocínio de cache do stage anterior: restaura antes de copiar o resto do código.
COPY src/OrcaLar.Api/OrcaLar.Api.csproj src/OrcaLar.Api/
RUN dotnet restore src/OrcaLar.Api/OrcaLar.Api.csproj
COPY src/OrcaLar.Api/ src/OrcaLar.Api/
# Só o projeto da API é publicado — os testes (tests/OrcaLar.Api.Tests) não fazem parte
# da imagem de produção.
RUN dotnet publish src/OrcaLar.Api/OrcaLar.Api.csproj -c Release -o /publish --no-restore

# ---- Stage 3: runtime ----------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build-back /publish .
# O front buildado vira os estáticos servidos pelo ASP.NET Core (UseStaticFiles lê de
# wwwroot por convenção).
COPY --from=build-front /web/dist ./wwwroot

# Documentação da porta padrão — não força porta nenhuma: o bind real usa a variável de
# ambiente PORT (fallback 8080), lida em Program.cs, igual em dev e em produção/Railway.
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrcaLar.Api.dll"]
