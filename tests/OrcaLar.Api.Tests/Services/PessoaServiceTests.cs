using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Tests.Services;

public class PessoaServiceTests
{
    [Fact]
    public async Task Deletar_DeveRemoverTransacoesEmCascata()
    {
        await using var dbContext = TestDbContextFactory.Criar();

        var pessoa = new Pessoa { Id = Guid.NewGuid(), Nome = "Ana", Idade = 30 };
        dbContext.Pessoas.Add(pessoa);
        dbContext.Transacoes.AddRange(
            new Transacao { Id = Guid.NewGuid(), Descricao = "Salário", Valor = 1000m, Tipo = TipoTransacao.Receita, PessoaId = pessoa.Id },
            new Transacao { Id = Guid.NewGuid(), Descricao = "Mercado", Valor = 200m, Tipo = TipoTransacao.Despesa, PessoaId = pessoa.Id });
        await dbContext.SaveChangesAsync();

        var service = new PessoaService(dbContext);
        var deletado = await service.DeletarAsync(pessoa.Id);

        Assert.True(deletado);
        Assert.Empty(dbContext.Pessoas);
        Assert.Empty(dbContext.Transacoes);
    }

    [Fact]
    public async Task Deletar_PessoaInexistente_DeveRetornarFalso()
    {
        await using var dbContext = TestDbContextFactory.Criar();
        var service = new PessoaService(dbContext);

        var deletado = await service.DeletarAsync(Guid.NewGuid());

        Assert.False(deletado);
    }
}
