using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.WorkflowManagement;
using FluentisCore.Models.InputAndApprovalManagement;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FluentisCore.DTO;
using FluentisCore.Auth;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ConditionalAuthorize] // Changed from custom policy to same as other controllers
    public class SolicitudesController : ControllerBase
    {
        private readonly FluentisContext _context;

        public SolicitudesController(FluentisContext context)
        {
            _context = context;
        }

        // GET: api/solicitudes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Solicitud>>> GetSolicitudes()
        {
            return await _context.Solicitudes
                .Include(s => s.Solicitante)
                .Include(s => s.FlujoBase)
                .Include(s => s.Inputs)
                .Include(s => s.GruposAprobacion)
                .ThenInclude(rga => rga.Decisiones)
                .ThenInclude(d => d.Usuario)
                .ToListAsync();
        }

        // GET: api/solicitudes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Solicitud>> GetSolicitud(int id)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.Solicitante)
                .Include(s => s.FlujoBase)
                .Include(s => s.Inputs)
                .Include(s => s.GruposAprobacion)
                .ThenInclude(rga => rga.Decisiones)
                .ThenInclude(d => d.Usuario)
                .FirstOrDefaultAsync(s => s.IdSolicitud == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            return solicitud;
        }

        // POST: api/solicitudes
        [HttpPost]
        public async Task<ActionResult<Solicitud>> CreateSolicitud([FromBody] SolicitudCreateDto solicitudDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var solicitud = new Solicitud
            {
                SolicitanteId = solicitudDto.SolicitanteId,
                FlujoBaseId = solicitudDto.FlujoBaseId,
                Nombre = solicitudDto.Nombre,
                Descripcion = solicitudDto.Descripcion,
                Estado = EstadoSolicitud.Pendiente,
                FechaCreacion = DateTime.Now
            };

            // Agregar inputs (pueden estar vacíos inicialmente)
            if (solicitudDto.Inputs != null)
            {
                solicitud.Inputs = new List<RelacionInput>();
                foreach (var inputDto in solicitudDto.Inputs)
                {
                    var input = new RelacionInput
                    {
                        InputId = inputDto.InputId,
                        Nombre = inputDto.Nombre,
                        PlaceHolder = inputDto.PlaceHolder,
                        Valor = inputDto.Valor, // Puede ser null o vacío
                        Requerido = inputDto.Requerido ?? false,
                        SolicitudId = solicitud.IdSolicitud // Se asignará después de guardar
                    };
                    solicitud.Inputs.Add(input);
                }
            }

            // Asociar grupo de aprobación
            if (solicitudDto.GrupoAprobacionId.HasValue)
            {
                solicitud.GruposAprobacion = new List<RelacionGrupoAprobacion>
                {
                    new RelacionGrupoAprobacion
                    {
                        GrupoAprobacionId = solicitudDto.GrupoAprobacionId.Value,
                        SolicitudId = solicitud.IdSolicitud // Se asignará después de guardar
                    }
                };
            }

            _context.Solicitudes.Add(solicitud);
            await _context.SaveChangesAsync();

            // Actualizar relaciones con el IdSolicitud generado
            if (solicitud.Inputs != null)
            {
                foreach (var input in solicitud.Inputs)
                {
                    input.SolicitudId = solicitud.IdSolicitud;
                }
            }
            if (solicitud.GruposAprobacion != null)
            {
                foreach (var grupo in solicitud.GruposAprobacion)
                {
                    grupo.SolicitudId = solicitud.IdSolicitud;
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSolicitud), new { id = solicitud.IdSolicitud }, solicitud);
        }

        // PUT: api/solicitudes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSolicitud(int id, [FromBody] SolicitudUpdateDto solicitudDto)
        {
            if (id != solicitudDto.IdSolicitud)
            {
                return BadRequest();
            }

            var solicitud = await _context.Solicitudes
                .Include(s => s.GruposAprobacion)
                .ThenInclude(rga => rga.Decisiones)
                .ThenInclude(d => d.Usuario)
                .FirstOrDefaultAsync(s => s.IdSolicitud == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            // Evaluar todas las decisiones de los grupos de aprobación
            bool todasAprobadas = true;
            bool algunaRechazada = false;

            foreach (var grupoAprobacion in solicitud.GruposAprobacion)
            {
                var decisiones = grupoAprobacion.Decisiones;
                if (decisiones.Any())
                {
                    if (!decisiones.All(d => d.Decision == true))
                    {
                        todasAprobadas = false;
                    }
                    if (decisiones.Any(d => d.Decision == false))
                    {
                        algunaRechazada = true;
                        break; // Si hay un rechazo, no necesita seguir evaluando
                    }
                }
                else
                {
                    todasAprobadas = false; // Si no hay decisiones, no está aprobado
                }
            }

            if (todasAprobadas && solicitud.GruposAprobacion.All(g => g.Decisiones.Any()))
            {
                solicitud.Estado = EstadoSolicitud.Aprobado;
            }
            else if (algunaRechazada)
            {
                solicitud.Estado = EstadoSolicitud.Rechazado;
            }
            else
            {
                solicitud.Estado = solicitudDto.Estado; // Permite cambiar manualmente si no hay decisiones completas
            }

            _context.Entry(solicitud).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SolicitudExists(id))
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

        // POST: api/solicitudes/5/inputs
        [HttpPost("{id}/inputs")]
        public async Task<ActionResult<RelacionInput>> AddInputToSolicitud(int id, [FromBody] RelacionInputCreateDto inputDto)
        {
            var solicitud = await _context.Solicitudes.FindAsync(id);
            if (solicitud == null)
            {
                return NotFound();
            }

            var input = new RelacionInput
            {
                InputId = inputDto.InputId,
                Nombre = inputDto.Nombre,
                PlaceHolder = inputDto.PlaceHolder,
                Valor = inputDto.Valor, // Puede ser null o vacío
                Requerido = inputDto.Requerido ?? false,
                SolicitudId = id
            };

            _context.RelacionesInput.Add(input);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSolicitud), new { id = solicitud.IdSolicitud }, input);
        }

        // POST: api/solicitudes/5/grupos-aprobacion
        [HttpPost("{id}/grupos-aprobacion")]
        public async Task<ActionResult<RelacionGrupoAprobacion>> AddGrupoAprobacionToSolicitud(int id, [FromBody] RelacionGrupoAprobacionCreateDto grupoDto)
        {
            var solicitud = await _context.Solicitudes.FindAsync(id);
            if (solicitud == null)
            {
                return NotFound();
            }

            var grupoRelacion = new RelacionGrupoAprobacion
            {
                GrupoAprobacionId = grupoDto.GrupoAprobacionId,
                SolicitudId = id
            };

            _context.RelacionesGrupoAprobacion.Add(grupoRelacion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSolicitud), new { id = solicitud.IdSolicitud }, grupoRelacion);
        }

        // POST: api/solicitudes/5/decision
        [HttpPost("{id}/decision")]
        public async Task<IActionResult> AddDecisionToSolicitud(int id, [FromBody] RelacionDecisionUsuarioCreateDto decisionDto)
        {
            var solicitud = await _context.Solicitudes
                .Include(s => s.GruposAprobacion)
                .ThenInclude(rga => rga.Decisiones)
                .ThenInclude(d => d.Usuario)
                .FirstOrDefaultAsync(s => s.IdSolicitud == id);

            if (solicitud == null)
            {
                return NotFound();
            }

            var grupoAprobacion = solicitud.GruposAprobacion.FirstOrDefault();
            if (grupoAprobacion == null)
            {
                return BadRequest("No hay un grupo de aprobación asociado.");
            }

            var decision = new RelacionDecisionUsuario
            {
                IdUsuario = decisionDto.IdUsuario,
                RelacionGrupoAprobacionId = grupoAprobacion.IdRelacion,
                Decision = decisionDto.Decision
            };

            _context.DecisionesUsuario.Add(decision);
            await _context.SaveChangesAsync();

            // Actualizar estado basado en todas las decisiones
            bool todasAprobadas = true;
            bool algunaRechazada = false;

            foreach (var ga in solicitud.GruposAprobacion)
            {
                var decisiones = ga.Decisiones;
                if (decisiones.Any())
                {
                    if (!decisiones.All(d => d.Decision == true))
                    {
                        todasAprobadas = false;
                    }
                    if (decisiones.Any(d => d.Decision == false))
                    {
                        algunaRechazada = true;
                        break;
                    }
                }
                else
                {
                    todasAprobadas = false;
                }
            }

            if (todasAprobadas && solicitud.GruposAprobacion.All(g => g.Decisiones.Any()))
            {
                solicitud.Estado = EstadoSolicitud.Aprobado;
            }
            else if (algunaRechazada)
            {
                solicitud.Estado = EstadoSolicitud.Rechazado;
            }

            await _context.SaveChangesAsync();

            return Ok(new { DecisionId = decision.IdRelacion, EstadoActual = solicitud.Estado });
        }

        // PUT: api/solicitudes/{id}/inputs/{inputId}
        [HttpPut("{id}/inputs/{inputId}")]
        public async Task<IActionResult> UpdateInputInSolicitud(int id, int inputId, [FromBody] RelacionInputUpdateDto inputDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var solicitud = await _context.Solicitudes.FindAsync(id);
            if (solicitud == null)
            {
                return NotFound("Solicitud no encontrada.");
            }

            var input = await _context.RelacionesInput
                .FirstOrDefaultAsync(ri => ri.SolicitudId == id && ri.IdRelacion == inputId);
            if (input == null)
            {
                return NotFound("Input no encontrado.");
            }

            // Actualizar solo los campos proporcionados, manteniendo los existentes si no se envían
            if (inputDto.Valor != null) input.Valor = inputDto.Valor; // Distingue entre null (no enviado) y "" (vacío)
            if (inputDto.PlaceHolder != null) input.PlaceHolder = inputDto.PlaceHolder;
            if (inputDto.Nombre != null) input.Nombre = inputDto.Nombre; // Nuevo: permite modificar Nombre
            if (inputDto.Requerido.HasValue) input.Requerido = inputDto.Requerido.Value;

            _context.Entry(input).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SolicitudExists(id))
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

        private bool SolicitudExists(int id)
        {
            return _context.Solicitudes.Any(e => e.IdSolicitud == id);
        }
    }
    
    
}