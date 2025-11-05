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
    public class DepartamentosController : ControllerBase
    {
        private readonly FluentisContext _context;

        public DepartamentosController(FluentisContext context)
        {
            _context = context;
        }

        // GET: api/Departamentos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartamentoDto>>> GetDepartamentos()
        {
            var result = await _context.Departamentos
                .Include(d => d.Usuarios)
                .Select(d => new DepartamentoDto
                {
                    IdDepartamento = d.IdDepartamento,
                    Nombre = d.Nombre,
                    Usuarios = d.Usuarios.Select(u => new UsuarioDto
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

        // GET: api/Departamentos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DepartamentoDto>> GetDepartamento(int id)
        {
            var departamento = await _context.Departamentos
                .Include(d => d.Usuarios)
                .Where(d => d.IdDepartamento == id)
                .Select(d => new DepartamentoDto
                {
                    IdDepartamento = d.IdDepartamento,
                    Nombre = d.Nombre,
                    Usuarios = d.Usuarios.Select(u => new UsuarioDto
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

            if (departamento == null)
            {
                return NotFound();
            }

            return departamento;
        }

        // PUT: api/Departamentos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDepartamento(int id, Departamento departamento)
        {
            if (id != departamento.IdDepartamento)
            {
                return BadRequest();
            }

            _context.Entry(departamento).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DepartamentoExists(id))
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

        // POST: api/Departamentos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Departamento>> PostDepartamento(Departamento departamento)
        {
            _context.Departamentos.Add(departamento);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDepartamento", new { id = departamento.IdDepartamento }, departamento);
        }

        // DELETE: api/Departamentos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartamento(int id)
        {
            var departamento = await _context.Departamentos.FindAsync(id);
            if (departamento == null)
            {
                return NotFound();
            }

            _context.Departamentos.Remove(departamento);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DepartamentoExists(int id)
        {
            return _context.Departamentos.Any(e => e.IdDepartamento == id);
        }
    }
}
