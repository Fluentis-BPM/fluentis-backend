using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            return await _context.GruposAprobacion.ToListAsync();
        }

        // GET: api/GrupoAprobaciones/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GrupoAprobacion>> GetGrupoAprobacion(int id)
        {
            var grupoAprobacion = await _context.GruposAprobacion.FindAsync(id);

            if (grupoAprobacion == null)
            {
                return NotFound();
            }

            return grupoAprobacion;
        }

        // PUT: api/GrupoAprobaciones/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGrupoAprobacion(int id, GrupoAprobacion grupoAprobacion)
        {
            if (id != grupoAprobacion.IdGrupo)
            {
                return BadRequest();
            }

            _context.Entry(grupoAprobacion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GrupoAprobacionExists(id))
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

        // POST: api/GrupoAprobaciones
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<GrupoAprobacion>> PostGrupoAprobacion(GrupoAprobacion grupoAprobacion)
        {
            _context.GruposAprobacion.Add(grupoAprobacion);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetGrupoAprobacion", new { id = grupoAprobacion.IdGrupo }, grupoAprobacion);
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
