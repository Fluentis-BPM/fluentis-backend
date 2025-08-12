using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.InputAndApprovalManagement;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FluentisCore.DTO;
using System; // for Exception
using Microsoft.Extensions.Logging; // added for ILogger

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class GrupoAprobacionesController : ControllerBase
    {
        private readonly FluentisContext _context;
        private readonly ILogger<GrupoAprobacionesController> _logger; // logger

        public GrupoAprobacionesController(FluentisContext context, ILogger<GrupoAprobacionesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/GrupoAprobaciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GrupoAprobacionListDto>>> GetGruposAprobacion()
        {
            try
            {
                var gruposQuery = _context.GruposAprobacion
                    .Include(g => g.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Rol)
                    .Include(g => g.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Departamento)
                    .Include(g => g.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Cargo)
                    .AsNoTracking();

                var grupos = await gruposQuery.ToListAsync();

                var dto = grupos.Select(g => new GrupoAprobacionListDto
                {
                    IdGrupo = g.IdGrupo,
                    Nombre = g.Nombre,
                    Fecha = g.Fecha,
                    EsGlobal = g.EsGlobal,
                    RelacionesUsuarioGrupo = g.RelacionesUsuarioGrupo?.Select(r => new GrupoAprobacionRelacionUsuarioDto
                    {
                        IdRelacion = r.IdRelacion,
                        GrupoAprobacionId = r.GrupoAprobacionId,
                        UsuarioId = r.UsuarioId,
                        Usuario = r.Usuario == null ? null : new GrupoAprobacionUsuarioDto
                        {
                            IdUsuario = r.Usuario.IdUsuario,
                            Nombre = r.Usuario.Nombre,
                            Email = r.Usuario.Email,
                            Rol = r.Usuario.Rol == null ? null : new SimpleRefDto { Id = r.Usuario.Rol.IdRol, Nombre = r.Usuario.Rol.Nombre },
                            Departamento = r.Usuario.Departamento == null ? null : new SimpleRefDto { Id = r.Usuario.Departamento.IdDepartamento, Nombre = r.Usuario.Departamento.Nombre },
                            Cargo = r.Usuario.Cargo == null ? null : new SimpleRefDto { Id = r.Usuario.Cargo.IdCargo, Nombre = r.Usuario.Cargo.Nombre }
                        }
                    }).ToList()
                }).ToList();

                return dto;
            }
            catch (Exception ex)
            {
                LogException(ex, "GET /api/GrupoAprobaciones");
                return StatusCode(500, "Error interno al obtener grupos de aprobación.");
            }
        }

        // GET: api/GrupoAprobaciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GrupoAprobacionListDto>> GetGrupoAprobacion(int id)
        {
            try
            {
                var g = await _context.GruposAprobacion
                    .Include(gr => gr.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Rol)
                    .Include(gr => gr.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Departamento)
                    .Include(gr => gr.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Cargo)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(gr => gr.IdGrupo == id);

                if (g == null) return NotFound();

                var dto = new GrupoAprobacionListDto
                {
                    IdGrupo = g.IdGrupo,
                    Nombre = g.Nombre,
                    Fecha = g.Fecha,
                    EsGlobal = g.EsGlobal,
                    RelacionesUsuarioGrupo = g.RelacionesUsuarioGrupo?.Select(r => new GrupoAprobacionRelacionUsuarioDto
                    {
                        IdRelacion = r.IdRelacion,
                        GrupoAprobacionId = r.GrupoAprobacionId,
                        UsuarioId = r.UsuarioId,
                        Usuario = r.Usuario == null ? null : new GrupoAprobacionUsuarioDto
                        {
                            IdUsuario = r.Usuario.IdUsuario,
                            Nombre = r.Usuario.Nombre,
                            Email = r.Usuario.Email,
                            Rol = r.Usuario.Rol == null ? null : new SimpleRefDto { Id = r.Usuario.Rol.IdRol, Nombre = r.Usuario.Rol.Nombre },
                            Departamento = r.Usuario.Departamento == null ? null : new SimpleRefDto { Id = r.Usuario.Departamento.IdDepartamento, Nombre = r.Usuario.Departamento.Nombre },
                            Cargo = r.Usuario.Cargo == null ? null : new SimpleRefDto { Id = r.Usuario.Cargo.IdCargo, Nombre = r.Usuario.Cargo.Nombre }
                        }
                    }).ToList()
                };
                return dto;
            }
            catch (Exception ex)
            {
                LogException(ex, $"GET /api/GrupoAprobaciones/{id}");
                return StatusCode(500, "Error interno al obtener el grupo.");
            }
        }

        // POST: api/GrupoAprobaciones
        [HttpPost]
        public async Task<ActionResult<GrupoAprobacionListDto>> PostGrupoAprobacion([FromBody] GrupoAprobacionCreateDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest("Payload requerido");
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (string.IsNullOrWhiteSpace(dto.Nombre))
                {
                    return BadRequest("El nombre del grupo es requerido.");
                }
                if (dto.UsuarioIds == null || !dto.UsuarioIds.Any())
                {
                    return BadRequest("Debe especificar al menos un usuario.");
                }

                var usuarios = await _context.Usuarios
                    .Where(u => dto.UsuarioIds.Contains(u.IdUsuario))
                    .Select(u => u.IdUsuario)
                    .ToListAsync();
                if (usuarios.Count != dto.UsuarioIds.Count)
                {
                    return BadRequest("Uno o más usuarios no existen.");
                }

                var grupo = new GrupoAprobacion
                {
                    Nombre = dto.Nombre.Trim(),
                    Fecha = DateTime.UtcNow,
                    EsGlobal = dto.EsGlobal ?? false
                };

                _context.GruposAprobacion.Add(grupo);
                await _context.SaveChangesAsync();

                foreach (var usuarioId in dto.UsuarioIds)
                {
                    _context.RelacionesUsuarioGrupo.Add(new RelacionUsuarioGrupo
                    {
                        GrupoAprobacionId = grupo.IdGrupo,
                        UsuarioId = usuarioId
                    });
                }
                await _context.SaveChangesAsync();

                var g = await _context.GruposAprobacion
                    .Include(gr => gr.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Rol)
                    .Include(gr => gr.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Departamento)
                    .Include(gr => gr.RelacionesUsuarioGrupo)
                        .ThenInclude(r => r.Usuario)
                            .ThenInclude(u => u.Cargo)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(gr => gr.IdGrupo == grupo.IdGrupo);

                if (g == null)
                {
                    return StatusCode(500, "No se pudo recargar el grupo creado.");
                }

                var resultDto = new GrupoAprobacionListDto
                {
                    IdGrupo = g.IdGrupo,
                    Nombre = g.Nombre,
                    Fecha = g.Fecha,
                    EsGlobal = g.EsGlobal,
                    RelacionesUsuarioGrupo = g.RelacionesUsuarioGrupo?.Select(r => new GrupoAprobacionRelacionUsuarioDto
                    {
                        IdRelacion = r.IdRelacion,
                        GrupoAprobacionId = r.GrupoAprobacionId,
                        UsuarioId = r.UsuarioId,
                        Usuario = r.Usuario == null ? null : new GrupoAprobacionUsuarioDto
                        {
                            IdUsuario = r.Usuario.IdUsuario,
                            Nombre = r.Usuario.Nombre,
                            Email = r.Usuario.Email,
                            Rol = r.Usuario.Rol == null ? null : new SimpleRefDto { Id = r.Usuario.Rol.IdRol, Nombre = r.Usuario.Rol.Nombre },
                            Departamento = r.Usuario.Departamento == null ? null : new SimpleRefDto { Id = r.Usuario.Departamento.IdDepartamento, Nombre = r.Usuario.Departamento.Nombre },
                            Cargo = r.Usuario.Cargo == null ? null : new SimpleRefDto { Id = r.Usuario.Cargo.IdCargo, Nombre = r.Usuario.Cargo.Nombre }
                        }
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetGrupoAprobacion), new { id = resultDto.IdGrupo }, resultDto);
            }
            catch (Exception ex)
            {
                LogException(ex, "POST /api/GrupoAprobaciones");
                return StatusCode(500, "Error interno al crear el grupo.");
            }
        }

        // PUT: api/GrupoAprobaciones/5
        [HttpPut("{id}")]
        public async Task<ActionResult<GrupoAprobacionListDto>> PutGrupoAprobacion(int id, [FromBody] GrupoAprobacionUpdateDto dto)
        {
            try
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

                if (!string.IsNullOrWhiteSpace(dto.Nombre))
                {
                    grupo.Nombre = dto.Nombre.Trim();
                }
                if (dto.EsGlobal.HasValue)
                {
                    grupo.EsGlobal = dto.EsGlobal.Value;
                }

                await _context.SaveChangesAsync();

                var recargado = await CargarGrupoConUsuarios(id);
                if (recargado == null) return NotFound();
                return MapGrupo(recargado);
            }
            catch (Exception ex)
            {
                LogException(ex, $"PUT /api/GrupoAprobaciones/{id}");
                return StatusCode(500, "Error interno al actualizar el grupo.");
            }
        }

        // POST: api/GrupoAprobaciones/5/usuarios
        [HttpPost("{id}/usuarios")]
        public async Task<ActionResult<GrupoAprobacionListDto>> AddUsuariosToGrupoAprobacion(int id, [FromBody] List<int> usuarioIds)
        {
            try
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
                    .Select(u => u.IdUsuario)
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
                    _context.RelacionesUsuarioGrupo.Add(new RelacionUsuarioGrupo
                    {
                        GrupoAprobacionId = id,
                        UsuarioId = usuarioId
                    });
                }

                await _context.SaveChangesAsync();
                var recargado = await CargarGrupoConUsuarios(id);
                if (recargado == null) return NotFound();
                return MapGrupo(recargado);
            }
            catch (Exception ex)
            {
                LogException(ex, $"POST /api/GrupoAprobaciones/{id}/usuarios");
                return StatusCode(500, "Error interno al agregar usuarios al grupo.");
            }
        }

        // DELETE: api/GrupoAprobaciones/5/usuarios/{usuarioId}
        [HttpDelete("{id}/usuarios/{usuarioId}")]
        public async Task<ActionResult<GrupoAprobacionListDto>> RemoveUsuarioFromGrupoAprobacion(int id, int usuarioId)
        {
            try
            {
                var relacion = await _context.RelacionesUsuarioGrupo
                    .FirstOrDefaultAsync(r => r.GrupoAprobacionId == id && r.UsuarioId == usuarioId);
                if (relacion == null)
                {
                    return NotFound("Relación usuario-grupo no encontrada.");
                }
                _context.RelacionesUsuarioGrupo.Remove(relacion);
                await _context.SaveChangesAsync();

                var recargado = await CargarGrupoConUsuarios(id);
                if (recargado == null) return NotFound();
                return MapGrupo(recargado);
            }
            catch (Exception ex)
            {
                LogException(ex, $"DELETE /api/GrupoAprobaciones/{id}/usuarios/{usuarioId}");
                return StatusCode(500, "Error interno al remover usuario del grupo.");
            }
        }

        // DELETE: api/GrupoAprobaciones/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGrupoAprobacion(int id)
        {
            try
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
            catch (Exception ex)
            {
                LogException(ex, $"DELETE /api/GrupoAprobaciones/{id}");
                return StatusCode(500, "Error interno al eliminar el grupo.");
            }
        }

        private async Task<GrupoAprobacion?> CargarGrupoConUsuarios(int id)
        {
            return await _context.GruposAprobacion
                .Include(gr => gr.RelacionesUsuarioGrupo)
                    .ThenInclude(r => r.Usuario)
                        .ThenInclude(u => u.Rol)
                .Include(gr => gr.RelacionesUsuarioGrupo)
                    .ThenInclude(r => r.Usuario)
                        .ThenInclude(u => u.Departamento)
                .Include(gr => gr.RelacionesUsuarioGrupo)
                    .ThenInclude(r => r.Usuario)
                        .ThenInclude(u => u.Cargo)
                .AsNoTracking()
                .FirstOrDefaultAsync(gr => gr.IdGrupo == id);
        }

        private static GrupoAprobacionListDto MapGrupo(GrupoAprobacion g)
        {
            return new GrupoAprobacionListDto
            {
                IdGrupo = g.IdGrupo,
                Nombre = g.Nombre,
                Fecha = g.Fecha,
                EsGlobal = g.EsGlobal,
                RelacionesUsuarioGrupo = g.RelacionesUsuarioGrupo?.Select(r => new GrupoAprobacionRelacionUsuarioDto
                {
                    IdRelacion = r.IdRelacion,
                    GrupoAprobacionId = r.GrupoAprobacionId,
                    UsuarioId = r.UsuarioId,
                    Usuario = r.Usuario == null ? null : new GrupoAprobacionUsuarioDto
                    {
                        IdUsuario = r.Usuario.IdUsuario,
                        Nombre = r.Usuario.Nombre,
                        Email = r.Usuario.Email,
                        Rol = r.Usuario.Rol == null ? null : new SimpleRefDto { Id = r.Usuario.Rol.IdRol, Nombre = r.Usuario.Rol.Nombre },
                        Departamento = r.Usuario.Departamento == null ? null : new SimpleRefDto { Id = r.Usuario.Departamento.IdDepartamento, Nombre = r.Usuario.Departamento.Nombre },
                        Cargo = r.Usuario.Cargo == null ? null : new SimpleRefDto { Id = r.Usuario.Cargo.IdCargo, Nombre = r.Usuario.Cargo.Nombre }
                    }
                }).ToList()
            };
        }

        private void LogException(Exception ex, string contexto)
        {
            _logger.LogError(ex, "Error en {Contexto}: {Mensaje}", contexto, ex.Message);
        }
    }
}