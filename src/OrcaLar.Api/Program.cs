using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;
using OrcaLar.Api.Infrastructure;
using OrcaLar.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// A porta é sempre lida do ambiente (Railway injeta PORT), com fallback para uso local.
// O bind em 0.0.0.0 (em vez de localhost) é necessário para aceitar conexões de fora do
// próprio container quando a API roda dockerizada.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Connection string sempre vem de variável de ambiente (nunca de appsettings), convertida
// do formato URI (DATABASE_URL) para o formato que o Npgsql entende.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = DatabaseUrlParser.ToNpgsqlConnectionString(databaseUrl);
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums (ex.: TipoTransacao) trafegam como string no JSON ("Receita"/"Despesa"),
        // não como número, para manter o contrato da API legível para quem consome.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Erros de validação de formato (DataAnnotations) do [ApiController] respondem, por padrão,
// com um ValidationProblemDetails próprio do ASP.NET Core. Aqui a resposta é substituída pelo
// envelope único de erro exigido pelo contrato da API: { "error": { "code", "message" } }.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var mensagens = context.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage);

        var envelope = new ErroEnvelope(new ErroDetalhe(
            CodigosErro.DadosInvalidos,
            string.Join(" ", mensagens)));

        return new BadRequestObjectResult(envelope);
    };
});

builder.Services.AddScoped<PessoaService>();
builder.Services.AddScoped<TransacaoService>();
builder.Services.AddScoped<TotaisService>();
builder.Services.AddScoped<FixaService>();
builder.Services.AddScoped<DashboardService>();

builder.Services.AddOpenApi();

var app = builder.Build();

await app.Services.MigrateWithRetryAsync(app.Logger);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthorization();

// Serve os estáticos da SPA (JS/CSS/imagens buildados pelo Vite, copiados para wwwroot
// no Dockerfile). Precisa vir antes de MapControllers/MapFallbackToFile: se a requisição
// bater num arquivo físico existente em wwwroot, esse middleware já responde e encerra o
// pipeline ali — sem isso, os assets do bundle (ex.: /assets/index-xxxx.js) cairiam no
// fallback abaixo e devolveriam o index.html no lugar do arquivo real.
app.UseStaticFiles();

// Rotas /api/* continuam com prioridade: endpoints mapeados por MapControllers (que casam
// por rota exata) sempre vencem o fallback abaixo, independente da ordem de registro — o
// roteamento do ASP.NET Core trata MapFallbackToFile como a opção de menor prioridade.
app.MapControllers();

// Qualquer rota que não seja /api/* nem um arquivo estático (ex.: /transacoes, /totais,
// ou um F5 direto nessas URLs) cai aqui e recebe o mesmo index.html — é o react-router,
// no cliente, quem decide qual tela mostrar. Sem isso, recarregar uma rota da SPA que não
// seja "/" devolveria 404, porque o servidor não conhece essas rotas.
app.MapFallbackToFile("index.html");

app.Run();
