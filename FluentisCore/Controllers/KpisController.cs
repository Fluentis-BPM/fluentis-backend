using System;
using System.Threading.Tasks;
using FluentisCore.Services;
using Microsoft.AspNetCore.Mvc;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KpisController : ControllerBase
    {
        private readonly IKpiService _kpiService;

        public KpisController(IKpiService kpiService)
        {
            _kpiService = kpiService;
        }

        // 1. Tiempo Promedio de Cierre de Flujos
        [HttpGet("tiempo-cierre-promedio")]
        public async Task<IActionResult> GetAvgFlowCloseTime([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] bool porFlujo = true)
        {
            var data = await _kpiService.GetAvgFlowCloseTimeAsync(startDate, endDate, porFlujo);
            return Ok(data);
        }

        // 2. Tiempo Promedio de Respuesta por Tipo de Paso
        [HttpGet("tiempo-respuesta-por-tipo")]
        public async Task<IActionResult> GetAvgStepResponseByType([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var data = await _kpiService.GetAvgStepResponseByTypeAsync(startDate, endDate);
            return Ok(data);
        }

        // 3. Volumen de Flujos por Mes
        [HttpGet("volumen-por-mes")]
        public async Task<IActionResult> GetFlowVolumeByMonth([FromQuery] int months = 12)
        {
            if (months <= 0) months = 12;
            var data = await _kpiService.GetFlowVolumeByMonthAsync(months);
            return Ok(data);
        }

        // 4. Cuellos de Botella
        [HttpGet("cuellos-de-botella")]
        public async Task<IActionResult> GetBottlenecks([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] string groupBy = "nombre", [FromQuery] int top = 10)
        {
            var data = await _kpiService.GetBottlenecksAsync(startDate, endDate, groupBy, top);
            return Ok(data);
        }

        // 5. Comparación Mes vs Mes Anterior
        [HttpGet("comparacion-mensual")]
        public async Task<IActionResult> GetMonthOverMonth([FromQuery] string? month = null)
        {
            var data = await _kpiService.GetMonthOverMonthAsync(month);
            return Ok(data);
        }

        // 6. Resumen de Flujos Activos
        [HttpGet("resumen-flujos")]
        public async Task<IActionResult> GetActiveFlowsSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var data = await _kpiService.GetActiveFlowsSummaryAsync(startDate, endDate);
            return Ok(data);
        }

        // 7. Resumen de Solicitudes
        [HttpGet("resumen-solicitudes")]
        public async Task<IActionResult> GetRequestsSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null, [FromQuery] int topN = 5)
        {
            var data = await _kpiService.GetRequestsSummaryAsync(startDate, endDate, topN);
            return Ok(data);
        }
    }
}
