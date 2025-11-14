using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentisCore.Auth;
using FluentisCore.DTO;
using FluentisCore.Models;
using FluentisCore.Models.CommentAndNotificationManagement;
using FluentisCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ConditionalAuthorize]
    public class NotificacionesController : ControllerBase
    {
        private readonly FluentisContext _context;
        private readonly NotificationService _notificationService;

        public NotificacionesController(FluentisContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // GET: api/Notificaciones
        // Filtros opcionales: usuarioId, soloNoLeidas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificacionDto>>> GetNotificaciones([FromQuery] int? usuarioId, [FromQuery] bool? soloNoLeidas)
        {
            var query = _context.Notificaciones
                .Include(n => n.Usuario)
                .AsQueryable();

            if (usuarioId.HasValue)
                query = query.Where(n => n.UsuarioId == usuarioId);
            if (soloNoLeidas.GetValueOrDefault())
                query = query.Where(n => !n.Leida);

            var result = await query
                .OrderByDescending(n => n.FechaEnvio)
                .Select(n => new NotificacionDto
                {
                    IdNotificacion = n.IdNotificacion,
                    UsuarioId = n.UsuarioId,
                    Mensaje = n.Mensaje,
                    Prioridad = n.Prioridad,
                    Leida = n.Leida,
                    FechaEnvio = n.FechaEnvio,
                    Usuario = n.Usuario == null ? null : new UsuarioMiniDto
                    {
                        IdUsuario = n.Usuario.IdUsuario,
                        Nombre = n.Usuario.Nombre,
                        Email = n.Usuario.Email
                    }
                })
                .ToListAsync();

            return result;
        }

        // GET: api/Notificaciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificacionDto>> GetNotificacion(int id)
        {
            var n = await _context.Notificaciones
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.IdNotificacion == id);
            if (n == null) return NotFound();

            return new NotificacionDto
            {
                IdNotificacion = n.IdNotificacion,
                UsuarioId = n.UsuarioId,
                Mensaje = n.Mensaje,
                Prioridad = n.Prioridad,
                Leida = n.Leida,
                FechaEnvio = n.FechaEnvio,
                Usuario = n.Usuario == null ? null : new UsuarioMiniDto
                {
                    IdUsuario = n.Usuario.IdUsuario,
                    Nombre = n.Usuario.Nombre,
                    Email = n.Usuario.Email
                }
            };
        }

        // POST: api/Notificaciones
        [HttpPost]
        public async Task<ActionResult<NotificacionDto>> PostNotificacion([FromBody] NotificacionCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var notificacion = await _notificationService.CrearNotificacionAsync(
                    dto.UsuarioId,
                    dto.Mensaje,
                    dto.Prioridad
                );

                return CreatedAtAction(nameof(GetNotificacion), new { id = notificacion.IdNotificacion }, new NotificacionDto
                {
                    IdNotificacion = notificacion.IdNotificacion,
                    UsuarioId = notificacion.UsuarioId,
                    Mensaje = notificacion.Mensaje,
                    Prioridad = notificacion.Prioridad,
                    Leida = notificacion.Leida,
                    FechaEnvio = notificacion.FechaEnvio
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        // PUT: api/Notificaciones/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNotificacion(int id, [FromBody] NotificacionUpdateDto dto)
        {
            var n = await _context.Notificaciones.FindAsync(id);
            if (n == null) return NotFound();

            if (dto.Mensaje != null) n.Mensaje = dto.Mensaje;
            if (dto.Prioridad.HasValue) n.Prioridad = dto.Prioridad.Value;
            if (dto.Leida.HasValue) n.Leida = dto.Leida.Value;

            _context.Entry(n).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Notificaciones/5/leer (marcar como leída)
        [HttpPost("{id}/leer")]
        public async Task<IActionResult> MarcarLeida(int id)
        {
            var resultado = await _notificationService.MarcarComoLeidaAsync(id);
            if (!resultado) return NotFound();
            return NoContent();
        }

        // POST: api/Notificaciones/leer-multiples (marcar varias como leídas)
        [HttpPost("leer-multiples")]
        public async Task<IActionResult> MarcarVariasLeidas([FromBody] List<int> notificacionesIds)
        {
            if (notificacionesIds == null || !notificacionesIds.Any())
                return BadRequest("Debe proporcionar al menos un ID de notificación.");

            var count = await _notificationService.MarcarVariasComoLeidasAsync(notificacionesIds);
            return Ok(new { marcadas = count });
        }

        // GET: api/Notificaciones/usuario/{usuarioId}/no-leidas-count
        [HttpGet("usuario/{usuarioId}/no-leidas-count")]
        public async Task<ActionResult<int>> GetNoLeidasCount(int usuarioId)
        {
            var count = await _notificationService.ObtenerNotificacionesNoLeidasCountAsync(usuarioId);
            return Ok(new { usuarioId, noLeidas = count });
        }

        // DELETE: api/Notificaciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotificacion(int id)
        {
            var n = await _context.Notificaciones.FindAsync(id);
            if (n == null) return NotFound();
            _context.Notificaciones.Remove(n);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
