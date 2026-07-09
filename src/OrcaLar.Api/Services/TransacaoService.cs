using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Dtos;
using OrcaLar.Api.Infrastructure;

namespace OrcaLar.Api.Services;

/// <summary>
/// Toda a regra de negócio de Transacao vive aqui — o Controller apenas orquestra.
/// </summary>
public class TransacaoService
{
    /// <summary>Idade a partir da qual uma pessoa pode ter receitas cadastradas em seu nome.</summary>
    private const int IdadeMinimaParaReceita = 18;

    private readonly AppDbContext _dbContext;

    public TransacaoService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransacaoDto> CriarAsync(string descricao, decimal valor, TipoTransacao tipo, Guid pessoaId)
    {
        var pessoa = await _dbContext.Pessoas.FirstOrDefaultAsync(p => p.Id == pessoaId);

        if (pessoa is null)
        {
            throw new RegraDeNegocioException(
                CodigosErro.PessoaNaoEncontrada,
                "A pessoa informada não foi encontrada.");
        }

        // Regra do enunciado: menor de idade não pode ter receita própria — só despesa.
        // Despesa é permitida para qualquer idade; receita exige 18 anos ou mais.
        if (tipo == TipoTransacao.Receita && pessoa.Idade < IdadeMinimaParaReceita)
        {
            throw new RegraDeNegocioException(
                CodigosErro.RegraMenorReceita,
                "Pessoas menores de 18 anos não podem cadastrar receitas.");
        }

        var transacao = new Transacao
        {
            Id = Guid.NewGuid(),
            Descricao = descricao,
            Valor = valor,
            Tipo = tipo,
            PessoaId = pessoaId
        };

        _dbContext.Transacoes.Add(transacao);
        await _dbContext.SaveChangesAsync();

        return ParaDto(transacao, pessoa.Nome);
    }

    /// <summary>Lista transações, com filtros opcionais por pessoa e por tipo.</summary>
    public async Task<IReadOnlyList<TransacaoDto>> ListarAsync(Guid? pessoaId, TipoTransacao? tipo)
    {
        var query = _dbContext.Transacoes.Include(t => t.Pessoa).AsNoTracking().AsQueryable();

        if (pessoaId.HasValue)
        {
            query = query.Where(t => t.PessoaId == pessoaId.Value);
        }

        if (tipo.HasValue)
        {
            query = query.Where(t => t.Tipo == tipo.Value);
        }

        var transacoes = await query.ToListAsync();

        return transacoes
            .Select(t => ParaDto(t, t.Pessoa.Nome))
            .ToList();
    }

    private static TransacaoDto ParaDto(Transacao transacao, string pessoaNome) => new(
        transacao.Id,
        transacao.Descricao,
        transacao.Valor,
        transacao.Tipo,
        transacao.PessoaId,
        pessoaNome);
}
