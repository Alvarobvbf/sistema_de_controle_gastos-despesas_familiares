using Microsoft.AspNetCore.Mvc;
using OrcaLar.Api.Dtos;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Controllers;

/// <summary>
/// Endpoints de Pessoa. Apenas orquestra: recebe o DTO, chama o Service e traduz o
/// resultado em status HTTP — nenhuma regra de negócio mora aqui.
/// </summary>
[ApiController]
[Route("api/pessoas")]
public class PessoasController : ControllerBase
{
    private readonly PessoaService _pessoaService;

    public PessoasController(PessoaService pessoaService)
    {
        _pessoaService = pessoaService;
    }

    [HttpPost]
    public async Task<ActionResult<PessoaDto>> Criar([FromBody] CriarPessoaRequest request)
    {
        var pessoa = await _pessoaService.CriarAsync(request.Nome, request.Idade!.Value);
        return Created($"/api/pessoas/{pessoa.Id}", pessoa);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PessoaDto>>> Listar([FromQuery] string? nome)
    {
        var pessoas = await _pessoaService.ListarAsync(nome);
        return Ok(pessoas);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deletar(Guid id)
    {
        var deletado = await _pessoaService.DeletarAsync(id);
        return deletado ? NoContent() : NotFound();
    }
}
