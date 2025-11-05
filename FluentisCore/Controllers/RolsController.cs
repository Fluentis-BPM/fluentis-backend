using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.UserManagement;
using FluentisCore.DTO;
using Microsoft.AspNetCore.Authorization;
using FluentisCore.Auth;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ConditionalAuthorize]
    public class RolsController : ControllerBase
    {
        private readonly FluentisContext _context;

        public RolsController(FluentisContext context)
        {
            _context = context;
        }

        // GET: api/Rols
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RolDto>>> GetRoles()
        {
            var result = await _context.Roles
                .Include(r => r.Usuarios)
                .Select(r => new RolDto
                {
                    IdRol = r.IdRol,
                    Nombre = r.Nombre,
                    Usuarios = r.Usuarios.Select(u => new UsuarioDto
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
                    }).ToList()
                })
                .ToListAsync();

            return result;
        }

        // GET: api/Rols/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RolDto>> GetRol(int id)
        {
            var rol = await _context.Roles
                .Include(r => r.Usuarios)
                .Where(r => r.IdRol == id)
                .Select(r => new RolDto
                {
                    IdRol = r.IdRol,
                    Nombre = r.Nombre,
                    Usuarios = r.Usuarios.Select(u => new UsuarioDto
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
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (rol == null)
            {
                return NotFound();
            }

            return rol;
        }

        // PUT: api/Rols/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRol(int id, Rol rol)
        {
            if (id != rol.IdRol)
            {
                return BadRequest();
            }

            _context.Entry(rol).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RolExists(id))
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

        // POST: api/Rols
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Rol>> PostRol(Rol rol)
        {
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRol", new { id = rol.IdRol }, rol);
        }

        // DELETE: api/Rols/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRol(int id)
        {
            var rol = await _context.Roles.FindAsync(id);
            if (rol == null)
            {
                return NotFound();
            }

            _context.Roles.Remove(rol);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RolExists(int id)
        {
            return _context.Roles.Any(e => e.IdRol == id);
        }
    }
}
