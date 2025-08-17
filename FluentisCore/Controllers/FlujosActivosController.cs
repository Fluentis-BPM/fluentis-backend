using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.WorkflowManagement;
using FluentisCore.DTO;
using FluentisCore.Extensions;
using FluentisCore.Services;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlujosActivosController : ControllerBase
    {
        private readonly FluentisContext _context;
        private readonly IWorkflowService _workflowService;

        public FlujosActivosController(FluentisContext context, IWorkflowService workflowService)
        {
            _context = context;
            _workflowService = workflowService;
        }

        // GET: api/FlujosActivos/pasos/{flujoActivoId}
        [HttpGet("Pasos/{flujoActivoId}")]
        public async Task<ActionResult<FlujoActivoResponseDto>> GetPasoSolicitudesByFlujoActivo(int flujoActivoId)
        {
            var flujoActivo = await _context.FlujosActivos.FindAsync(flujoActivoId);
            if (flujoActivo == null)
            {
                return NotFound("Flujo activo no encontrado.");
            }

            var pasos = await _context.PasosSolicitud
                .Include(p => p.RelacionesInput)
                .Include(p => p.RelacionesGrupoAprobacion)
                .Include(p => p.Comentarios)
                .Include(p => p.Excepciones)
                .Where(p => p.FlujoActivoId == flujoActivoId)
                .ToListAsync();

            foreach (var paso in pasos)
            {
                paso.TipoFlujo = await _workflowService.GetTipoFlujo(paso.IdPasoSolicitud, _context);
            }

            var caminos = await _context.CaminosParalelos
                .Where(c => pasos.Select(p => p.PasoId).Contains(c.PasoOrigenId)
                          || pasos.Select(p => p.PasoId).Contains(c.PasoDestinoId))
                .ToListAsync();

            var response = new FlujoActivoResponseDto
            {
                FlujoActivoId = flujoActivoId,
                Pasos = pasos.Select(p => p.ToFrontendDto()).ToList(),
                Caminos = caminos.Select(c => c.ToFrontendDto()).ToList()
            };

            return response;
        }

        // GET: api/FlujosActivos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FlujoActivoFrontendDto>>> GetFlujosActivos()
        {
            var list = await _context.FlujosActivos
                .Include(f => f.FlujoEjecucion)
                .Include(f => f.Solicitud)
                .ToListAsync();
            return list.Select(f => f.ToFrontendDto()).ToList();
        }

        // GET: api/FlujosActivos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FlujoActivoFrontendDto>> GetFlujoActivo(int id)
        {
            var flujoActivo = await _context.FlujosActivos
                .Include(f => f.FlujoEjecucion)
                .Include(f => f.Solicitud)
                .FirstOrDefaultAsync(f => f.IdFlujoActivo == id);
            if (flujoActivo == null) return NotFound();
            return flujoActivo.ToFrontendDto();
        }

        // PUT: api/FlujosActivos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFlujoActivo(int id, [FromBody] FlujoActivoUpdateDto dto)
        {
            var flujo = await _context.FlujosActivos.FindAsync(id);
            if (flujo == null) return NotFound();
            flujo.ApplyUpdate(dto);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlujoActivoExists(id)) return NotFound();
                throw;
            }
            return NoContent();
        }

        // POST: api/FlujosActivos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<FlujoActivoFrontendDto>> PostFlujoActivo([FromBody] FlujoActivoCreateDto dto)
        {
            var model = dto.ToModel();
            _context.FlujosActivos.Add(model);
            await _context.SaveChangesAsync();
            await _context.Entry(model).Reference(m => m.FlujoEjecucion).LoadAsync();
            await _context.Entry(model).Reference(m => m.Solicitud).LoadAsync();
            return CreatedAtAction(nameof(GetFlujoActivo), new { id = model.IdFlujoActivo }, model.ToFrontendDto());
        }

        // DELETE: api/FlujosActivos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlujoActivo(int id)
        {
            var flujoActivo = await _context.FlujosActivos.FindAsync(id);
            if (flujoActivo == null)
            {
                return NotFound();
            }

            _context.FlujosActivos.Remove(flujoActivo);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FlujoActivoExists(int id)
        {
            return _context.FlujosActivos.Any(e => e.IdFlujoActivo == id);
        }
    }
}
