using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.WorkflowManagement;
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

        // GET: api/FlujosActivos/pasos/{id}
        [HttpGet("Pasos/{id}")]
        public async Task<ActionResult<IEnumerable<PasoSolicitud>>> GetPasoSolicitudesByFlujoActivo(int flujoActivoId)
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

            return pasos;
        }

        // GET: api/FlujosActivos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FlujoActivo>>> GetFlujosActivos()
        {
            return await _context.FlujosActivos.ToListAsync();
        }

        // GET: api/FlujosActivos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FlujoActivo>> GetFlujoActivo(int id)
        {
            var flujoActivo = await _context.FlujosActivos.FindAsync(id);

            if (flujoActivo == null)
            {
                return NotFound();
            }

            return flujoActivo;
        }

        // PUT: api/FlujosActivos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFlujoActivo(int id, FlujoActivo flujoActivo)
        {
            if (id != flujoActivo.IdFlujoActivo)
            {
                return BadRequest();
            }

            _context.Entry(flujoActivo).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlujoActivoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/FlujosActivos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<FlujoActivo>> PostFlujoActivo(FlujoActivo flujoActivo)
        {
            _context.FlujosActivos.Add(flujoActivo);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFlujoActivo", new { id = flujoActivo.IdFlujoActivo }, flujoActivo);
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
