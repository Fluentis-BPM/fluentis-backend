using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.DTO;
using FluentisCore.Extensions;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RelacionInputsController : ControllerBase
    {
        private readonly FluentisContext _context;

        public RelacionInputsController(FluentisContext context)
        {
            _context = context;
        }

        // GET: api/RelacionInputs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RelacionInputDto>>> GetRelacionesInput()
        {
            var relaciones = await _context.RelacionesInput
                .Include(r => r.Input)
                .ToListAsync();
            
            return relaciones.Select(r => r.ToDto()).ToList();
        }

        // GET: api/RelacionInputs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RelacionInputDto>> GetRelacionInput(int id)
        {
            var relacionInput = await _context.RelacionesInput
                .Include(r => r.Input)
                .FirstOrDefaultAsync(r => r.IdRelacion == id);

            if (relacionInput == null)
            {
                return NotFound();
            }

            return relacionInput.ToDto();
        }

        // PUT: api/RelacionInputs/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRelacionInput(int id, RelacionInput relacionInput)
        {
            if (id != relacionInput.IdRelacion)
            {
                return BadRequest();
            }

            _context.Entry(relacionInput).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RelacionInputExists(id))
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

        // POST: api/RelacionInputs
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<RelacionInput>> PostRelacionInput(RelacionInput relacionInput)
        {
            _context.RelacionesInput.Add(relacionInput);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRelacionInput", new { id = relacionInput.IdRelacion }, relacionInput);
        }

        // DELETE: api/RelacionInputs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRelacionInput(int id)
        {
            var relacionInput = await _context.RelacionesInput.FindAsync(id);
            if (relacionInput == null)
            {
                return NotFound();
            }

            _context.RelacionesInput.Remove(relacionInput);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RelacionInputExists(int id)
        {
            return _context.RelacionesInput.Any(e => e.IdRelacion == id);
        }
    }
}
