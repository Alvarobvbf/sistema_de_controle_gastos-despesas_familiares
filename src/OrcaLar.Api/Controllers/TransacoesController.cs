using Microsoft.AspNetCore.Mvc;
using OrcaLar.Api.Domain.Entities;
using OrcaLar.Api.Dtos;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Controllers;

/// <summary>
/// Endpoints de Transacao. Apenas orquestra: recebe o DTO, chama o Service e traduz o
/// resultado em status HTTP — nenhuma regra de negócio mora aqui.
/// </summary>
[ApiController]
[Route("api/transacoes")]
public class TransacoesController : ControllerBase
{
    private readonly TransacaoService _transacaoService;

    public TransacoesController(TransacaoService transacaoService)
    {
        _transacaoService = transacaoService;
    }

    [HttpPost]
    public async Task<ActionResult<TransacaoDto>> Criar([FromBody] CriarTransacaoRequest request)
    {
        var transacao = await _transacaoService.CriarAsync(
            request.Descricao,
            request.Valor,
            request.Tipo!.Value,
            request.PessoaId);

        return Created($"/api/transacoes/{transacao.Id}", transacao);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransacaoDto>>> Listar(
        [FromQuery] Guid? pessoaId,
        [FromQuery] TipoTransacao? tipo)
    {
        var transacoes = await _transacaoService.ListarAsync(pessoaId, tipo);
        return Ok(transacoes);
    }
}
