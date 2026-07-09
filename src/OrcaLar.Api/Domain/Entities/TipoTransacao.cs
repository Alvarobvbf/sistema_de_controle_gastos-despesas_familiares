namespace OrcaLar.Api.Domain.Entities;

/// <summary>
/// Tipo de uma transação financeira: entrada de dinheiro (Receita) ou saída (Despesa).
/// Serializado como string no JSON (ver JsonStringEnumConverter em Program.cs) para
/// manter o contrato da API legível, em vez de expor o índice numérico do enum.
/// </summary>
public enum TipoTransacao
{
    Despesa,
    Receita
}
