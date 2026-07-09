using Microsoft.EntityFrameworkCore;
using OrcaLar.Api.Domain.Entities;

namespace OrcaLar.Api.Data;

/// <summary>
/// Contexto EF Core do OrçaLar. Mapeia Pessoa e Transacao para o PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Pessoa> Pessoas => Set<Pessoa>();

    public DbSet<Transacao> Transacoes => Set<Transacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pessoa>(pessoa =>
        {
            pessoa.HasKey(p => p.Id);
            pessoa.Property(p => p.Nome).IsRequired();
            pessoa.Property(p => p.Idade).IsRequired();
        });

        modelBuilder.Entity<Transacao>(transacao =>
        {
            transacao.HasKey(t => t.Id);
            transacao.Property(t => t.Descricao).IsRequired();

            // decimal com precisão fixa no banco (numeric(18,2)): nunca usar float/double
            // para dinheiro, para evitar erros de arredondamento em somas de totais.
            transacao.Property(t => t.Valor).HasColumnType("numeric(18,2)");

            // Enum persistido como string também no banco, para manter os dados legíveis
            // diretamente em uma consulta SQL (consistente com a serialização JSON).
            transacao.Property(t => t.Tipo).HasConversion<string>();

            // Cascade garantido no nível do banco (FK ON DELETE CASCADE): deletar uma Pessoa
            // apaga suas Transacoes mesmo que a deleção não passe pelo EF (ex.: script SQL direto).
            transacao.HasOne(t => t.Pessoa)
                .WithMany(p => p.Transacoes)
                .HasForeignKey(t => t.PessoaId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
