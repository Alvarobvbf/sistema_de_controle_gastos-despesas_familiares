using OrcaLar.Api.Data;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Infrastructure;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Tests.Services;

public class TransacaoServiceTests
{
    [Fact]
    public async Task Criar_ComPessoaInexistente_DeveRejeitar()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var service = new TransacaoService(dbContext);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            service.CriarAsync("Salário", 100m, TipoTransacao.Receita, Guid.NewGuid(), data: null));

        Assert.Equal(CodigosErro.PessoaNaoEncontrada, excecao.Code);
    }

    [Fact]
    public async Task Criar_ReceitaParaMenorDeIdade_DeveRejeitar()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 17);
        var service = new TransacaoService(dbContext);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            service.CriarAsync("Mesada", 50m, TipoTransacao.Receita, pessoa.Id, data: null));

        Assert.Equal(CodigosErro.RegraMenorReceita, excecao.Code);
    }

    [Fact]
    public async Task Criar_DespesaParaMenorDeIdade_DevePermitir()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 17);
        var service = new TransacaoService(dbContext);

        var transacao = await service.CriarAsync("Lanche", 20m, TipoTransacao.Despesa, pessoa.Id, data: null);

        Assert.Equal(TipoTransacao.Despesa, transacao.Tipo);
        Assert.Equal(pessoa.Id, transacao.PessoaId);
    }

    [Theory]
    [InlineData(TipoTransacao.Receita)]
    [InlineData(TipoTransacao.Despesa)]
    public async Task Criar_ParaPessoaComDezoitoAnos_DevePermitirAmbosOsTipos(TipoTransacao tipo)
    {
        await using var dbContext = TestDbContextFactory.Criar();
        // 18 é o limite exato em que receita passa a ser permitida (a regra é "idade < 18").
        var pessoa = await CriarPessoaAsync(dbContext, idade: 18);
        var service = new TransacaoService(dbContext);

        var transacao = await service.CriarAsync("Transação", 30m, tipo, pessoa.Id, data: null);

        Assert.Equal(tipo, transacao.Tipo);
    }

    [Fact]
    public async Task Criar_SemDataInformada_UsaDataAtualDoServidor()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 30);
        var service = new TransacaoService(dbContext);

        var transacao = await service.CriarAsync("Mercado", 100m, TipoTransacao.Despesa, pessoa.Id, data: null);

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), transacao.Data);
    }

    [Fact]
    public async Task Criar_ComDataInformada_UsaADataInformada()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 30);
        var service = new TransacaoService(dbContext);
        var dataEscolhida = new DateOnly(2026, 1, 15);

        var transacao = await service.CriarAsync("Presente de aniversário", 200m, TipoTransacao.Despesa, pessoa.Id, dataEscolhida);

        Assert.Equal(dataEscolhida, transacao.Data);
    }

    [Fact]
    public async Task Listar_DeveOrdenarPorDataDecrescente()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 30);
        var service = new TransacaoService(dbContext);

        await service.CriarAsync("Mais antiga", 10m, TipoTransacao.Despesa, pessoa.Id, new DateOnly(2026, 1, 1));
        await service.CriarAsync("Mais recente", 20m, TipoTransacao.Despesa, pessoa.Id, new DateOnly(2026, 3, 1));
        await service.CriarAsync("Intermediária", 30m, TipoTransacao.Despesa, pessoa.Id, new DateOnly(2026, 2, 1));

        var transacoes = await service.ListarAsync(pessoaId: null, tipo: null);

        Assert.Equal(["Mais recente", "Intermediária", "Mais antiga"], transacoes.Select(t => t.Descricao));
    }

    private static async Task<Pessoa> CriarPessoaAsync(AppDbContext dbContext, int idade)
    {
        var pessoa = new Pessoa { Id = Guid.NewGuid(), Nome = "Pessoa Teste", Idade = idade };
        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync();
        return pessoa;
    }
}
