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
            service.CriarAsync("Salário", 100m, TipoTransacao.Receita, Guid.NewGuid()));

        Assert.Equal(CodigosErro.PessoaNaoEncontrada, excecao.Code);
    }

    [Fact]
    public async Task Criar_ReceitaParaMenorDeIdade_DeveRejeitar()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 17);
        var service = new TransacaoService(dbContext);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            service.CriarAsync("Mesada", 50m, TipoTransacao.Receita, pessoa.Id));

        Assert.Equal(CodigosErro.RegraMenorReceita, excecao.Code);
    }

    [Fact]
    public async Task Criar_DespesaParaMenorDeIdade_DevePermitir()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 17);
        var service = new TransacaoService(dbContext);

        var transacao = await service.CriarAsync("Lanche", 20m, TipoTransacao.Despesa, pessoa.Id);

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

        var transacao = await service.CriarAsync("Transação", 30m, tipo, pessoa.Id);

        Assert.Equal(tipo, transacao.Tipo);
    }

    private static async Task<Pessoa> CriarPessoaAsync(AppDbContext dbContext, int idade)
    {
        var pessoa = new Pessoa { Id = Guid.NewGuid(), Nome = "Pessoa Teste", Idade = idade };
        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync();
        return pessoa;
    }
}
