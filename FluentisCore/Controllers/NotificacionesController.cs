using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentisCore.Auth;
using FluentisCore.DTO;
using FluentisCore.Models;
using FluentisCore.Models.CommentAndNotificationManagement;
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

        public NotificacionesController(FluentisContext context)
        {
            _context = context;
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

            var existsUsuario = await _context.Usuarios.AnyAsync(u => u.IdUsuario == dto.UsuarioId);
            if (!existsUsuario) return NotFound("Usuario no encontrado.");

            var model = new Notificacion
            {
                UsuarioId = dto.UsuarioId,
                Mensaje = dto.Mensaje,
                Prioridad = dto.Prioridad,
                Leida = false
                // FechaEnvio es generado por la BD si está configurado como Computed
            };

            _context.Notificaciones.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNotificacion), new { id = model.IdNotificacion }, new NotificacionDto
            {
                IdNotificacion = model.IdNotificacion,
                UsuarioId = model.UsuarioId,
                Mensaje = model.Mensaje,
                Prioridad = model.Prioridad,
                Leida = model.Leida,
                FechaEnvio = model.FechaEnvio
            });
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
            var n = await _context.Notificaciones.FindAsync(id);
            if (n == null) return NotFound();
            if (!n.Leida)
            {
                n.Leida = true;
                _context.Entry(n).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            return NoContent();
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
