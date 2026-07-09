using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Tests.Services;

public class TotaisServiceTests
{
    [Fact]
    public async Task Obter_DeveCalcularTotaisPorPessoaESaldoGeral_IncluindoPessoaSemTransacoes()
    {
        await using var dbContext = TestDbContextFactory.Criar();

        var joao = new Pessoa { Id = Guid.NewGuid(), Nome = "João", Idade = 40 };
        var maria = new Pessoa { Id = Guid.NewGuid(), Nome = "Maria", Idade = 35 };
        var semTransacoes = new Pessoa { Id = Guid.NewGuid(), Nome = "Sem Transações", Idade = 25 };

        dbContext.Pessoas.AddRange(joao, maria, semTransacoes);
        dbContext.Transacoes.AddRange(
            new Transacao { Id = Guid.NewGuid(), Descricao = "Salário", Valor = 5000m, Tipo = TipoTransacao.Receita, PessoaId = joao.Id },
            new Transacao { Id = Guid.NewGuid(), Descricao = "Aluguel", Valor = 1500m, Tipo = TipoTransacao.Despesa, PessoaId = joao.Id },
            new Transacao { Id = Guid.NewGuid(), Descricao = "Freelance", Valor = 800m, Tipo = TipoTransacao.Receita, PessoaId = maria.Id },
            new Transacao { Id = Guid.NewGuid(), Descricao = "Mercado", Valor = 300m, Tipo = TipoTransacao.Despesa, PessoaId = maria.Id });
        await dbContext.SaveChangesAsync();

        var service = new TotaisService(dbContext);
        var totais = await service.ObterAsync();

        Assert.Equal(3, totais.Pessoas.Count);

        var totalJoao = totais.Pessoas.Single(p => p.PessoaId == joao.Id);
        Assert.Equal(5000m, totalJoao.TotalReceitas);
        Assert.Equal(1500m, totalJoao.TotalDespesas);
        Assert.Equal(3500m, totalJoao.Saldo);

        var totalMaria = totais.Pessoas.Single(p => p.PessoaId == maria.Id);
        Assert.Equal(800m, totalMaria.TotalReceitas);
        Assert.Equal(300m, totalMaria.TotalDespesas);
        Assert.Equal(500m, totalMaria.Saldo);

        var totalSemTransacoes = totais.Pessoas.Single(p => p.PessoaId == semTransacoes.Id);
        Assert.Equal(0m, totalSemTransacoes.TotalReceitas);
        Assert.Equal(0m, totalSemTransacoes.TotalDespesas);
        Assert.Equal(0m, totalSemTransacoes.Saldo);

        Assert.Equal(5800m, totais.TotalGeral.TotalReceitas);
        Assert.Equal(1800m, totais.TotalGeral.TotalDespesas);
        Assert.Equal(4000m, totais.TotalGeral.SaldoLiquido);
    }
}
