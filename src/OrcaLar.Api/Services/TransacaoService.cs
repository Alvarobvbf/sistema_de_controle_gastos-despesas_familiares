using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Dtos;

namespace OrcaLar.Api.Services;

/// <summary>
/// Toda a regra de negócio de Transacao vive aqui — o Controller apenas orquestra.
/// </summary>
public class TransacaoService
{
    private readonly AppDbContext _dbContext;

    public TransacaoService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransacaoDto> CriarAsync(
        string descricao, decimal valor, TipoTransacao tipo, Guid pessoaId, DateOnly? data)
    {
        var pessoa = await RegrasLancamento.BuscarPessoaOuFalharAsync(_dbContext, pessoaId);
        RegrasLancamento.ValidarTipoParaIdade(tipo, pessoa.Idade);

        var transacao = new Transacao
        {
            Id = Guid.NewGuid(),
            Descricao = descricao,
            Valor = valor,
            Tipo = tipo,
            PessoaId = pessoaId,
            // "Hoje" vem sempre do servidor quando a data é omitida — fonte única de verdade,
            // nunca do relógio do cliente (que pode estar errado ou em outro fuso).
            Data = data ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };

        _dbContext.Transacoes.Add(transacao);
        await _dbContext.SaveChangesAsync();

        return ParaDto(transacao, pessoa.Nome);
    }

    /// <summary>Lista transações, com filtros opcionais por pessoa e por tipo, mais recentes primeiro.</summary>
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

        var transacoes = await query.OrderByDescending(t => t.Data).ToListAsync();

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
        pessoaNome,
        transacao.Data);
}
