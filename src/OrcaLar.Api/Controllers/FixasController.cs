using Microsoft.AspNetCore.Mvc;
using OrcaLar.Api.Dtos;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Controllers;

/// <summary>
/// Endpoints de Fixa. Apenas orquestra: recebe o DTO, chama o Service e traduz o resultado
/// em status HTTP — nenhuma regra de negócio mora aqui.
/// </summary>
[ApiController]
[Route("api/fixas")]
public class FixasController : ControllerBase
{
    private readonly FixaService _fixaService;

    public FixasController(FixaService fixaService)
    {
        _fixaService = fixaService;
    }

    [HttpPost]
    public async Task<ActionResult<FixaDto>> Criar([FromBody] CriarFixaRequest request)
    {
        var fixa = await _fixaService.CriarAsync(
            request.Descricao,
            request.Valor,
            request.Tipo!.Value,
            request.PessoaId,
            request.DiaDoMes,
            request.DataInicio,
            request.DataFim);

        return Created($"/api/fixas/{fixa.Id}", fixa);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FixaDto>>> Listar([FromQuery] Guid? pessoaId)
    {
        var fixas = await _fixaService.ListarAsync(pessoaId);
        return Ok(fixas);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var deletado = await _fixaService.DeletarAsync(id);
        return deletado ? NoContent() : NotFound();
    }
}
