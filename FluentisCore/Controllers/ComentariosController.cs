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
    public class ComentariosController : ControllerBase
    {
        private readonly FluentisContext _context;

        public ComentariosController(FluentisContext context)
        {
            _context = context;
        }

        // GET: api/Comentarios
        // Admite filtros opcionales: pasoSolicitudId, flujoActivoId, usuarioId
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ComentarioDto>>> GetComentarios([FromQuery] int? pasoSolicitudId, [FromQuery] int? flujoActivoId, [FromQuery] int? usuarioId)
        {
            var query = _context.Comentarios
                .Include(c => c.Usuario)
                .AsQueryable();

            if (pasoSolicitudId.HasValue)
                query = query.Where(c => c.PasoSolicitudId == pasoSolicitudId);
            if (flujoActivoId.HasValue)
                query = query.Where(c => c.FlujoActivoId == flujoActivoId);
            if (usuarioId.HasValue)
                query = query.Where(c => c.UsuarioId == usuarioId);

            var result = await query
                .OrderByDescending(c => c.Fecha)
                .Select(c => new ComentarioDto
                {
                    IdComentario = c.IdComentario,
                    PasoSolicitudId = c.PasoSolicitudId,
                    FlujoActivoId = c.FlujoActivoId,
                    UsuarioId = c.UsuarioId,
                    Contenido = c.Contenido,
                    Fecha = c.Fecha,
                    Usuario = c.Usuario == null ? null : new UsuarioMiniDto
                    {
                        IdUsuario = c.Usuario.IdUsuario,
                        Nombre = c.Usuario.Nombre,
                        Email = c.Usuario.Email
                    }
                })
                .ToListAsync();

            return result;
        }

        // GET: api/Comentarios/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ComentarioDto>> GetComentario(int id)
        {
            var c = await _context.Comentarios
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.IdComentario == id);

            if (c == null) return NotFound();

            var dto = new ComentarioDto
            {
                IdComentario = c.IdComentario,
                PasoSolicitudId = c.PasoSolicitudId,
                FlujoActivoId = c.FlujoActivoId,
                UsuarioId = c.UsuarioId,
                Contenido = c.Contenido,
                Fecha = c.Fecha,
                Usuario = c.Usuario == null ? null : new UsuarioMiniDto
                {
                    IdUsuario = c.Usuario.IdUsuario,
                    Nombre = c.Usuario.Nombre,
                    Email = c.Usuario.Email
                }
            };
            return dto;
        }

        // POST: api/Comentarios
        [HttpPost]
        public async Task<ActionResult<ComentarioDto>> PostComentario([FromBody] ComentarioCreateGeneralDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!dto.PasoSolicitudId.HasValue && !dto.FlujoActivoId.HasValue)
            {
                return BadRequest("Debe especificar PasoSolicitudId o FlujoActivoId.");
            }

            // Validar existencia de referencias
            if (dto.PasoSolicitudId.HasValue)
            {
                var existsPaso = await _context.PasosSolicitud.AnyAsync(p => p.IdPasoSolicitud == dto.PasoSolicitudId.Value);
                if (!existsPaso) return NotFound("Paso de solicitud no encontrado.");
            }
            if (dto.FlujoActivoId.HasValue)
            {
                var existsFlujo = await _context.FlujosActivos.AnyAsync(f => f.IdFlujoActivo == dto.FlujoActivoId.Value);
                if (!existsFlujo) return NotFound("Flujo activo no encontrado.");
            }
            var existsUsuario = await _context.Usuarios.AnyAsync(u => u.IdUsuario == dto.UsuarioId);
            if (!existsUsuario) return NotFound("Usuario no encontrado.");

            var model = new Comentario
            {
                PasoSolicitudId = dto.PasoSolicitudId,
                FlujoActivoId = dto.FlujoActivoId,
                UsuarioId = dto.UsuarioId,
                Contenido = dto.Contenido,
                Fecha = System.DateTime.UtcNow
            };

            _context.Comentarios.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetComentario), new { id = model.IdComentario }, new ComentarioDto
            {
                IdComentario = model.IdComentario,
                PasoSolicitudId = model.PasoSolicitudId,
                FlujoActivoId = model.FlujoActivoId,
                UsuarioId = model.UsuarioId,
                Contenido = model.Contenido,
                Fecha = model.Fecha
            });
        }

        // PUT: api/Comentarios/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutComentario(int id, [FromBody] ComentarioUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var c = await _context.Comentarios.FindAsync(id);
            if (c == null) return NotFound();

            c.Contenido = dto.Contenido;
            _context.Entry(c).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Comentarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComentario(int id)
        {
            var c = await _context.Comentarios.FindAsync(id);
            if (c == null) return NotFound();
            _context.Comentarios.Remove(c);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
