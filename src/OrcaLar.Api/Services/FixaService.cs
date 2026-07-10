using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Dtos;

namespace OrcaLar.Api.Services;

/// <summary>
/// Toda a regra de negócio de Fixa vive aqui — o Controller apenas orquestra. As checagens
/// de pessoa-existe e menor-não-tem-receita são as mesmas de Transacao (ver RegrasLancamento).
/// </summary>
public class FixaService
{
    private readonly AppDbContext _dbContext;

    public FixaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FixaDto> CriarAsync(
        string descricao, decimal valor, TipoTransacao tipo, Guid pessoaId,
        int diaDoMes, DateOnly? dataInicio, DateOnly? dataFim)
    {
        var pessoa = await RegrasLancamento.BuscarPessoaOuFalharAsync(_dbContext, pessoaId);
        RegrasLancamento.ValidarTipoParaIdade(tipo, pessoa.Idade);

        var fixa = new Fixa
        {
            Id = Guid.NewGuid(),
            Descricao = descricao,
            Valor = valor,
            Tipo = tipo,
            PessoaId = pessoaId,
            DiaDoMes = diaDoMes,
            // Mesmo critério de Transacao: "hoje" vem do servidor quando omitido.
            DataInicio = dataInicio ?? DateOnly.FromDateTime(DateTime.UtcNow),
            DataFim = dataFim
        };

        _dbContext.Fixas.Add(fixa);
        await _dbContext.SaveChangesAsync();

        return ParaDto(fixa, pessoa.Nome);
    }

    /// <summary>Lista fixas, com filtro opcional por pessoa.</summary>
    public async Task<IReadOnlyList<FixaDto>> ListarAsync(Guid? pessoaId)
    {
        var query = _dbContext.Fixas.Include(f => f.Pessoa).AsNoTracking().AsQueryable();

        if (pessoaId.HasValue)
        {
            query = query.Where(f => f.PessoaId == pessoaId.Value);
        }

        var fixas = await query.ToListAsync();

        return fixas
            .Select(f => ParaDto(f, f.Pessoa.Nome))
            .ToList();
    }

    /// <summary>Retorna false se a fixa não existir, para o Controller devolver 404.</summary>
    public async Task<bool> DeletarAsync(Guid id)
    {
        var fixa = await _dbContext.Fixas.FirstOrDefaultAsync(f => f.Id == id);

        if (fixa is null)
        {
            return false;
        }

        _dbContext.Fixas.Remove(fixa);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static FixaDto ParaDto(Fixa fixa, string pessoaNome) => new(
        fixa.Id,
        fixa.Descricao,
        fixa.Valor,
        fixa.Tipo,
        fixa.PessoaId,
        pessoaNome,
        fixa.DiaDoMes,
        fixa.DataInicio,
        fixa.DataFim);
}
