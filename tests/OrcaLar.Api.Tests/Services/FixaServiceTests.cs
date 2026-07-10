using OrcaLar.Api.Data;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Infrastructure;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Tests.Services;

public class FixaServiceTests
{
    [Fact]
    public async Task Criar_ComPessoaInexistente_DeveRejeitar()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var service = new FixaService(dbContext);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            service.CriarAsync("Salário", 3000m, TipoTransacao.Receita, Guid.NewGuid(), diaDoMes: 5, dataInicio: null, dataFim: null));

        Assert.Equal(CodigosErro.PessoaNaoEncontrada, excecao.Code);
    }

    [Fact]
    public async Task Criar_ReceitaParaMenorDeIdade_DeveRejeitar()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 15);
        var service = new FixaService(dbContext);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(() =>
            service.CriarAsync("Mesada", 100m, TipoTransacao.Receita, pessoa.Id, diaDoMes: 5, dataInicio: null, dataFim: null));

        Assert.Equal(CodigosErro.RegraMenorReceita, excecao.Code);
    }

    [Fact]
    public async Task Criar_DespesaParaMenorDeIdade_DevePermitir()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 15);
        var service = new FixaService(dbContext);

        var fixa = await service.CriarAsync("Mensalidade escola", 500m, TipoTransacao.Despesa, pessoa.Id, diaDoMes: 10, dataInicio: null, dataFim: null);

        Assert.Equal(TipoTransacao.Despesa, fixa.Tipo);
    }

    [Fact]
    public async Task Criar_SemDataInicioInformada_UsaDataAtualDoServidor()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 30);
        var service = new FixaService(dbContext);

        var fixa = await service.CriarAsync("Aluguel", 1200m, TipoTransacao.Despesa, pessoa.Id, diaDoMes: 5, dataInicio: null, dataFim: null);

        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), fixa.DataInicio);
        Assert.Null(fixa.DataFim);
    }

    [Fact]
    public async Task Listar_ComFiltroDePessoa_RetornaSomenteAsDaquelaPessoa()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var joao = await CriarPessoaAsync(dbContext, idade: 30);
        var maria = await CriarPessoaAsync(dbContext, idade: 30);
        var service = new FixaService(dbContext);

        await service.CriarAsync("Aluguel", 1200m, TipoTransacao.Despesa, joao.Id, diaDoMes: 5, dataInicio: null, dataFim: null);
        await service.CriarAsync("Salário", 3000m, TipoTransacao.Receita, maria.Id, diaDoMes: 1, dataInicio: null, dataFim: null);

        var fixasDeJoao = await service.ListarAsync(joao.Id);

        Assert.Single(fixasDeJoao);
        Assert.Equal("Aluguel", fixasDeJoao[0].Descricao);
    }

    [Fact]
    public async Task Deletar_FixaExistente_Remove()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var pessoa = await CriarPessoaAsync(dbContext, idade: 30);
        var service = new FixaService(dbContext);
        var fixa = await service.CriarAsync("Aluguel", 1200m, TipoTransacao.Despesa, pessoa.Id, diaDoMes: 5, dataInicio: null, dataFim: null);

        var deletou = await service.DeletarAsync(fixa.Id);

        Assert.True(deletou);
        Assert.Empty(await service.ListarAsync(pessoaId: null));
    }

    [Fact]
    public async Task Deletar_FixaInexistente_RetornaFalse()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var service = new FixaService(dbContext);

        var deletou = await service.DeletarAsync(Guid.NewGuid());

        Assert.False(deletou);
    }

    private static async Task<Pessoa> CriarPessoaAsync(AppDbContext dbContext, int idade)
    {
        var pessoa = new Pessoa { Id = Guid.NewGuid(), Nome = "Pessoa Teste", Idade = idade };
        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync();
        return pessoa;
    }
}
