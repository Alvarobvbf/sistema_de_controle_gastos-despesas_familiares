using Microsoft.AspNetCore.Mvc;
using OrcaLar.Api.Dtos;
using OrcaLar.Api.Services;

namespace OrcaLar.Api.Controllers;

/// <summary>
/// Endpoint de série temporal do dashboard. Separado de /api/totais: aquele reflete só o
/// realizado; este combina realizado (histórico) com a projeção de Fixas.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("series")]
    public async Task<ActionResult<SerieResponse>> Series(
        [FromQuery] Granularidade granularidade = Granularidade.Mes,
        [FromQuery] int mesesProjecao = 6)
    {
        var series = await _dashboardService.ObterSeriesAsync(granularidade, mesesProjecao);
        return Ok(series);
    }
}
