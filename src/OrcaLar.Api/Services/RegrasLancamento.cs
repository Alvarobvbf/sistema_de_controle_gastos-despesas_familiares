using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Infrastructure;

namespace OrcaLar.Api.Services;

/// <summary>
/// Regras de negócio compartilhadas entre TransacaoService e FixaService — ambos são
/// "lançamentos" associados a uma Pessoa e sujeitos exatamente às mesmas duas checagens
/// (pessoa existe, menor de idade não tem receita). Centralizado aqui para não duplicar a
/// mesma lógica nos dois Services (DRY), sem virar um repositório genérico.
/// </summary>
public static class RegrasLancamento
{
    /// <summary>Idade a partir da qual uma pessoa pode ter receitas cadastradas em seu nome.</summary>
    public const int IdadeMinimaParaReceita = 18;

    public static async Task<Pessoa> BuscarPessoaOuFalharAsync(AppDbContext dbContext, Guid pessoaId)
    {
        var pessoa = await dbContext.Pessoas.FirstOrDefaultAsync(p => p.Id == pessoaId);

        if (pessoa is null)
        {
            throw new RegraDeNegocioException(
                CodigosErro.PessoaNaoEncontrada,
                "A pessoa informada não foi encontrada.");
        }

        return pessoa;
    }

    /// <summary>Despesa é permitida para qualquer idade; receita exige 18 anos ou mais.</summary>
    public static void ValidarTipoParaIdade(TipoTransacao tipo, int idade)
    {
        if (tipo == TipoTransacao.Receita && idade < IdadeMinimaParaReceita)
        {
            throw new RegraDeNegocioException(
                CodigosErro.RegraMenorReceita,
                "Pessoas menores de 18 anos não podem cadastrar receitas.");
        }
    }
}
