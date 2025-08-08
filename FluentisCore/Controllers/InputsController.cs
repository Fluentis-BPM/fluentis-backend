using FluentisCore.DTO;
using FluentisCore.Models;
using FluentisCore.Models.InputAndApprovalManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class InputsController : ControllerBase
    {
        private readonly FluentisContext _context;

        public InputsController(FluentisContext context)
        {
            _context = context;
        }

        // GET: api/inputs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Inputs>>> GetInputs()
        {
            return await _context.Inputs.ToListAsync();
        }

        // GET: api/inputs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Inputs>> GetInput(int id)
        {
            var input = await _context.Inputs.FindAsync(id);

            if (input == null)
            {
                return NotFound();
            }

            return input;
        }

        // POST: api/inputs
        [HttpPost]
        public async Task<ActionResult<Inputs>> CreateInput([FromBody] InputCreateDto inputDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var input = new Inputs
            {
                EsJson = inputDto.EsJson,
                TipoInput = inputDto.TipoInput
            };

            _context.Inputs.Add(input);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetInput), new { id = input.IdInput }, input);
        }

        // PUT: api/inputs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInput(int id, [FromBody] InputUpdateDto inputDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var input = await _context.Inputs.FindAsync(id);
            if (input == null)
            {
                return NotFound();
            }

            input.EsJson = inputDto.EsJson;
            input.TipoInput = inputDto.TipoInput;

            _context.Entry(input).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InputExists(id))
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

        // DELETE: api/inputs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInput(int id)
        {
            var input = await _context.Inputs.FindAsync(id);
            if (input == null)
            {
                return NotFound();
            }

            _context.Inputs.Remove(input);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool InputExists(int id)
        {
            return _context.Inputs.Any(e => e.IdInput == id);
        }
    }
}