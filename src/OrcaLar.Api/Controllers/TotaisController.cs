using Microsoft.AspNetCore.Mvc;
using OrcaLar.Api.Dtos;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Controllers;

/// <summary>Endpoint de consulta de totais. Apenas orquestra — o cálculo mora no Service.</summary>
[ApiController]
[Route("api/totais")]
public class TotaisController : ControllerBase
{
    private readonly TotaisService _totaisService;

    public TotaisController(TotaisService totaisService)
    {
        _totaisService = totaisService;
    }

    [HttpGet]
    public async Task<ActionResult<TotaisResponse>> Obter()
    {
        var totais = await _totaisService.ObterAsync();
        return Ok(totais);
    }
}
