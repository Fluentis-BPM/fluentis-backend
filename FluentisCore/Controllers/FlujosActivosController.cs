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
        private readonly NotificationService _notificationService;

        public FlujosActivosController(FluentisContext context, IWorkflowService workflowService, NotificationService notificationService)
        {
            _context = context;
            _workflowService = workflowService;
            _notificationService = notificationService;
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
                    .ThenInclude(rga => rga.GrupoAprobacion)
                        .ThenInclude(ga => ga.RelacionesUsuarioGrupo)
                            .ThenInclude(rug => rug.Usuario)
                .Include(p => p.RelacionesGrupoAprobacion)
                    .ThenInclude(rga => rga.Decisiones)
                    .ThenInclude(d => d.Usuario)
                .Include(p => p.Comentarios)
                .Include(p => p.Excepciones)
                .Where(p => p.FlujoActivoId == flujoActivoId)
                .ToListAsync();

            foreach (var paso in pasos)
            {
                paso.TipoFlujo = await _workflowService.GetTipoFlujo(paso.IdPasoSolicitud, _context);
            }

            var conexiones = await _context.ConexionesPasoSolicitud
                .Where(c => pasos.Select(p => p.IdPasoSolicitud).Contains(c.PasoOrigenId)
                          || pasos.Select(p => p.IdPasoSolicitud).Contains(c.PasoDestinoId))
                .ToListAsync();

            var response = new FlujoActivoResponseDto
            {
                FlujoActivoId = flujoActivoId,
                Pasos = pasos.Select(p => p.ToFrontendDto()).ToList(),
                Caminos = conexiones.Select(c => new CaminoParaleloFrontendDto
                {
                    IdCamino = c.IdConexion,
                    PasoOrigenId = c.PasoOrigenId,
                    PasoDestinoId = c.PasoDestinoId,
                    EsExcepcion = c.EsExcepcion,
                    Nombre = null
                }).ToList()
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

        // GET: api/FlujosActivos/usuario/{usuarioId}
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<FlujoActivoFrontendDto>>> GetFlujosByUsuario(
            int usuarioId,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] EstadoFlujoActivo? estado = null)
        {
            // Verificar que el usuario existe
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            // Llamar al servicio para obtener los flujos
            var resultado = await _workflowService.GetFlujosByUsuario(
                usuarioId, 
                fechaInicio, 
                fechaFin, 
                estado, 
                _context);

            return Ok(resultado);
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

            // Notificar inicio del flujo
            try
            {
                // Obtener el primer paso activo del flujo para notificar a su responsable
                var primerPaso = await _context.PasosSolicitud
                    .Where(p => p.FlujoActivoId == model.IdFlujoActivo && 
                                p.Estado == EstadoPasoSolicitud.Pendiente &&
                                p.ResponsableId.HasValue)
                    .OrderBy(p => p.FechaInicio)
                    .FirstOrDefaultAsync();

                if (primerPaso != null && primerPaso.ResponsableId.HasValue)
                {
                    var nombreSolicitud = model.Solicitud?.Nombre ?? "";
                    await _notificationService.NotificarInicioFlujoAsync(
                        primerPaso.ResponsableId.Value,
                        model.Nombre ?? "flujo",
                        nombreSolicitud
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al notificar inicio de flujo: {ex.Message}");
            }

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

        // POST: api/FlujosActivos/{id}/visualizadores
        [HttpPost("{id}/visualizadores")]
        public async Task<IActionResult> AddVisualizadores(int id, [FromBody] List<int> usuarioIds)
        {
            // Verificar que el flujo activo existe
            var flujoActivo = await _context.FlujosActivos.FindAsync(id);
            if (flujoActivo == null)
            {
                return NotFound(new { message = "Flujo activo no encontrado." });
            }

            if (usuarioIds == null || !usuarioIds.Any())
            {
                return BadRequest(new { message = "Debe proporcionar al menos un ID de usuario." });
            }

            var errores = new List<string>();
            var agregados = new List<int>();

            foreach (var usuarioId in usuarioIds)
            {
                // Verificar que el usuario existe
                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null)
                {
                    errores.Add($"Usuario con ID {usuarioId} no encontrado.");
                    continue;
                }

                // Verificar si ya existe la relación
                var relacionExistente = await _context.RelacionesVisualizadores
                    .FirstOrDefaultAsync(rv => rv.FlujoActivoId == id && rv.UsuarioId == usuarioId);

                if (relacionExistente != null)
                {
                    errores.Add($"Usuario con ID {usuarioId} ya es visualizador de este flujo.");
                    continue;
                }

                // Crear la relación
                var nuevaRelacion = new FluentisCore.Models.InputAndApprovalManagement.RelacionVisualizador
                {
                    FlujoActivoId = id,
                    UsuarioId = usuarioId
                };

                _context.RelacionesVisualizadores.Add(nuevaRelacion);
                agregados.Add(usuarioId);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Se agregaron {agregados.Count} visualizador(es) al flujo activo.",
                agregados,
                errores = errores.Any() ? errores : null
            });
        }

        // GET: api/FlujosActivos/{id}/visualizadores
        [HttpGet("{id}/visualizadores")]
        public async Task<ActionResult<IEnumerable<object>>> GetVisualizadores(int id)
        {
            var flujoActivo = await _context.FlujosActivos.FindAsync(id);
            if (flujoActivo == null)
            {
                return NotFound(new { message = "Flujo activo no encontrado." });
            }

            var visualizadores = await _context.RelacionesVisualizadores
                .Where(rv => rv.FlujoActivoId == id)
                .Include(rv => rv.Usuario)
                    .ThenInclude(u => u.Departamento)
                .Include(rv => rv.Usuario)
                    .ThenInclude(u => u.Cargo)
                .Select(rv => new
                {
                    idRelacion = rv.IdRelacion,
                    usuarioId = rv.UsuarioId,
                    nombre = rv.Usuario.Nombre,
                    email = rv.Usuario.Email,
                    departamento = rv.Usuario.Departamento != null ? rv.Usuario.Departamento.Nombre : null,
                    cargo = rv.Usuario.Cargo != null ? rv.Usuario.Cargo.Nombre : null
                })
                .ToListAsync();

            return Ok(visualizadores);
        }

        // DELETE: api/FlujosActivos/{id}/visualizadores/{usuarioId}
        [HttpDelete("{id}/visualizadores/{usuarioId}")]
        public async Task<IActionResult> RemoveVisualizador(int id, int usuarioId)
        {
            var relacion = await _context.RelacionesVisualizadores
                .FirstOrDefaultAsync(rv => rv.FlujoActivoId == id && rv.UsuarioId == usuarioId);

            if (relacion == null)
            {
                return NotFound(new { message = "La relación de visualizador no existe." });
            }

            _context.RelacionesVisualizadores.Remove(relacion);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Visualizador removido exitosamente." });
        }

        private bool FlujoActivoExists(int id)
        {
            return _context.FlujosActivos.Any(e => e.IdFlujoActivo == id);
        }
    }
}
