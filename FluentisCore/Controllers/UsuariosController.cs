using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentisCore.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.UserManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using FluentisCore.DTO;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ConditionalAuthorize]
    public class UsuariosController : ControllerBase
    {
        private readonly FluentisContext _context;

        public UsuariosController(FluentisContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UsuarioDto>>> GetUsuarios()
        {
            return await _context.Usuarios
                .Include(u => u.Departamento)
                .Include(u => u.Rol)
                .Include(u => u.Cargo)
                .Select(u => new UsuarioDto
                {
                    IdUsuario = u.IdUsuario,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    Oid = u.Oid,
                    DepartamentoId = u.DepartamentoId,
                    DepartamentoNombre = u.Departamento != null ? u.Departamento.Nombre : null,
                    RolId = u.RolId,
                    RolNombre = u.Rol != null ? u.Rol.Nombre : null,
                    CargoId = u.CargoId,
                    CargoNombre = u.Cargo != null ? u.Cargo.Nombre : null
                })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UsuarioDto>> GetUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Departamento)
                .Include(u => u.Rol)
                .Include(u => u.Cargo)
                .Select(u => new UsuarioDto
                {
                    IdUsuario = u.IdUsuario,
                    Nombre = u.Nombre,
                    Email = u.Email,
                    Oid = u.Oid,
                    DepartamentoId = u.DepartamentoId,
                    DepartamentoNombre = u.Departamento != null ? u.Departamento.Nombre : null,
                    RolId = u.RolId,
                    RolNombre = u.Rol != null ? u.Rol.Nombre : null,
                    CargoId = u.CargoId,
                    CargoNombre = u.Cargo != null ? u.Cargo.Nombre : null
                })
                .FirstOrDefaultAsync(u => u.IdUsuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return usuario;
        }

        // PUT: api/Usuarios/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUsuario(int id, Usuario usuario)
        {
            if (id != usuario.IdUsuario)
            {
                return BadRequest();
            }

            _context.Entry(usuario).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(id))
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

        // POST: api/Usuarios
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(UsuarioCreateDto usuarioDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usuario = new Usuario
            {
                Nombre = usuarioDto.Nombre,
                Email = usuarioDto.Email,
                Oid = usuarioDto.Oid,
                DepartamentoId = usuarioDto.DepartamentoId,
                CargoId = usuarioDto.CargoId,
                RolId = usuarioDto.RolId
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUsuario", new { id = usuario.IdUsuario }, usuario);
        }

        // DELETE: api/Usuarios/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }

        // PUT: api/Usuarios/{id}/departamento
        [HttpPut("{id}/departamento")]
        public async Task<ActionResult<UsuarioDto>> SetUsuarioDepartamento(int id, [FromBody] SetDepartamentoDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.DepartamentoId = dto.DepartamentoId; // null => unassign
            await _context.SaveChangesAsync();

            return await GetUsuario(id);
        }

        // PUT: api/Usuarios/{id}/rol
        [HttpPut("{id}/rol")]
        public async Task<ActionResult<UsuarioDto>> SetUsuarioRol(int id, [FromBody] SetRolDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.RolId = dto.RolId; // null => unassign
            await _context.SaveChangesAsync();

            return await GetUsuario(id);
        }

        // PUT: api/Usuarios/{id}/cargo
        [HttpPut("{id}/cargo")]
        public async Task<ActionResult<UsuarioDto>> SetUsuarioCargo(int id, [FromBody] SetCargoDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.CargoId = dto.CargoId; // null => unassign
            await _context.SaveChangesAsync();

            return await GetUsuario(id);
        }
    }
}
