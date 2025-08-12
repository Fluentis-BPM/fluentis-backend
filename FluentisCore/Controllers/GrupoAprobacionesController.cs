using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.InputAndApprovalManagement;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FluentisCore.DTO;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class GrupoAprobacionesController : ControllerBase
    {
        private readonly FluentisContext _context;

        public GrupoAprobacionesController(FluentisContext context)
        {
            _context = context;
        }

        // GET: api/GrupoAprobaciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GrupoAprobacion>>> GetGruposAprobacion()
        {
            return await _context.GruposAprobacion
                .Include(g => g.RelacionesUsuarioGrupo)
                .ThenInclude(r => r.Usuario)
                .ToListAsync();
        }

        // GET: api/GrupoAprobaciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GrupoAprobacion>> GetGrupoAprobacion(int id)
        {
            var grupoAprobacion = await _context.GruposAprobacion
                .Include(g => g.RelacionesUsuarioGrupo)
                .ThenInclude(r => r.Usuario)
                .FirstOrDefaultAsync(g => g.IdGrupo == id);

            if (grupoAprobacion == null)
            {
                return NotFound();
            }

            return grupoAprobacion;
        }

        // POST: api/GrupoAprobaciones
        [HttpPost]
        public async Task<ActionResult<GrupoAprobacion>> PostGrupoAprobacion([FromBody] GrupoAprobacionCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (dto.UsuarioIds == null || !dto.UsuarioIds.Any())
            {
                return BadRequest("Debe especificar al menos un usuario.");
            }

            var usuarios = await _context.Usuarios
                .Where(u => dto.UsuarioIds.Contains(u.IdUsuario))
                .ToListAsync();
            if (usuarios.Count != dto.UsuarioIds.Count)
            {
                return BadRequest("Uno o más usuarios no existen.");
            }

            var grupo = new GrupoAprobacion
            {
                Nombre = dto.Nombre,
                Fecha = DateTime.UtcNow,
                EsGlobal = dto.EsGlobal ?? false
            };

            _context.GruposAprobacion.Add(grupo);
            await _context.SaveChangesAsync();

            // Crear relaciones con usuarios
            foreach (var usuarioId in dto.UsuarioIds)
            {
                var relacion = new RelacionUsuarioGrupo
                {
                    GrupoAprobacionId = grupo.IdGrupo,
                    UsuarioId = usuarioId
                };
                _context.RelacionesUsuarioGrupo.Add(relacion);
            }
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGrupoAprobacion), new { id = grupo.IdGrupo }, grupo);
        }

        // PUT: api/GrupoAprobaciones/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGrupoAprobacion(int id, [FromBody] GrupoAprobacionUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var grupo = await _context.GruposAprobacion.FindAsync(id);
            if (grupo == null)
            {
                return NotFound();
            }

            grupo.Nombre = dto.Nombre ?? grupo.Nombre;
            grupo.EsGlobal = dto.EsGlobal ?? grupo.EsGlobal;

            _context.Entry(grupo).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/GrupoAprobaciones/5/usuarios
        [HttpPost("{id}/usuarios")]
        public async Task<IActionResult> AddUsuariosToGrupoAprobacion(int id, [FromBody] List<int> usuarioIds)
        {
            if (usuarioIds == null || !usuarioIds.Any())
            {
                return BadRequest("Debe especificar al menos un usuario.");
            }

            var grupo = await _context.GruposAprobacion.FindAsync(id);
            if (grupo == null)
            {
                return NotFound("Grupo no encontrado.");
            }

            var usuarios = await _context.Usuarios
                .Where(u => usuarioIds.Contains(u.IdUsuario))
                .ToListAsync();
            if (usuarios.Count != usuarioIds.Count)
            {
                return BadRequest("Uno o más usuarios no existen.");
            }

            var relacionesExistentes = await _context.RelacionesUsuarioGrupo
                .Where(r => r.GrupoAprobacionId == id)
                .Select(r => r.UsuarioId)
                .ToListAsync();
            var nuevosUsuarios = usuarioIds.Except(relacionesExistentes).ToList();

            foreach (var usuarioId in nuevosUsuarios)
            {
                var relacion = new RelacionUsuarioGrupo
                {
                    GrupoAprobacionId = id,
                    UsuarioId = usuarioId
                };
                _context.RelacionesUsuarioGrupo.Add(relacion);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/GrupoAprobaciones/5/usuarios/{usuarioId}
        [HttpDelete("{id}/usuarios/{usuarioId}")]
        public async Task<IActionResult> RemoveUsuarioFromGrupoAprobacion(int id, int usuarioId)
        {
            var relacion = await _context.RelacionesUsuarioGrupo
                .FirstOrDefaultAsync(r => r.GrupoAprobacionId == id && r.UsuarioId == usuarioId);
            if (relacion == null)
            {
                return NotFound("Relación usuario-grupo no encontrada.");
            }

            _context.RelacionesUsuarioGrupo.Remove(relacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/GrupoAprobaciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrupoAprobacion(int id)
        {
            var grupoAprobacion = await _context.GruposAprobacion.FindAsync(id);
            if (grupoAprobacion == null)
            {
                return NotFound();
            }

            _context.GruposAprobacion.Remove(grupoAprobacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool GrupoAprobacionExists(int id)
        {
            return _context.GruposAprobacion.Any(e => e.IdGrupo == id);
        }
    }
}