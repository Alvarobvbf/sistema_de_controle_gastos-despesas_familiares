using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Data;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Dtos;

namespace OrcaLar.Api.Services;

/// <summary>
/// Toda a regra de negócio de Pessoa vive aqui — o Controller apenas orquestra.
/// </summary>
public class PessoaService
{
    private readonly AppDbContext _dbContext;

    public PessoaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PessoaDto> CriarAsync(string nome, int idade)
    {
        var pessoa = new Pessoa
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            Idade = idade
        };

        _dbContext.Pessoas.Add(pessoa);
        await _dbContext.SaveChangesAsync();

        return ParaDto(pessoa);
    }

    /// <summary>Busca parcial e case-insensitive pelo nome; sem filtro, lista todas.</summary>
    public async Task<IReadOnlyList<PessoaDto>> ListarAsync(string? nome)
    {
        var pessoas = await _dbContext.Pessoas.AsNoTracking().ToListAsync();

        if (!string.IsNullOrWhiteSpace(nome))
        {
            pessoas = pessoas
                .Where(p => p.Nome.Contains(nome, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return pessoas
            .OrderBy(p => p.Nome)
            .Select(ParaDto)
            .ToList();
    }

    /// <summary>
    /// Deleta a pessoa e, em cascata, todas as suas transações e fixas. Ambas precisam estar
    /// carregadas (Include) para que o EF Core aplique o cascade delete também no nível do
    /// change tracker — a constraint ON DELETE CASCADE no banco cobre o caso de deleção direta
    /// via SQL, mas o comportamento em memória (usado nos testes) depende do tracking.
    /// Retorna false se a pessoa não existir, para o Controller devolver 404.
    /// </summary>
    public async Task<bool> DeletarAsync(Guid id)
    {
        var pessoa = await _dbContext.Pessoas
            .Include(p => p.Transacoes)
            .Include(p => p.Fixas)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (pessoa is null)
        {
            return false;
        }

        _dbContext.Pessoas.Remove(pessoa);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    private static PessoaDto ParaDto(Pessoa pessoa) => new(pessoa.Id, pessoa.Nome, pessoa.Idade);
}
