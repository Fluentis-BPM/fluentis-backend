using FluentisCore.Auth;
using FluentisCore.DTO;
using FluentisCore.Extensions;
using FluentisCore.Models;
using FluentisCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ConditionalAuthorize]
    public class PlantillasController : ControllerBase
    {
        private readonly ITemplateService _service;
        private readonly FluentisContext _context;

        public PlantillasController(ITemplateService service, FluentisContext context)
        {
            _service = service;
            _context = context;
        }

        /// <summary>
        /// Lista todas las plantillas de solicitud.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlantillaSolicitudDto>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        /// <summary>
        /// Obtiene una plantilla por Id.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PlantillaSolicitudDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Crea una nueva plantilla de solicitud (solo administradores).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PlantillaSolicitudDto>> Create([FromBody] PlantillaSolicitudCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.IdPlantilla }, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = "Error de base de datos al crear la plantilla", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// Actualiza una plantilla existente.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<PlantillaSolicitudDto>> Update(int id, [FromBody] PlantillaSolicitudUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (dto.IdPlantilla != 0 && dto.IdPlantilla != id) return BadRequest("Id mismatch");
            dto.IdPlantilla = id;
            try
            {
                var updated = await _service.UpdateAsync(id, dto);
                if (updated == null) return NotFound();
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = "Error de base de datos al actualizar la plantilla", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }

        /// <summary>
        /// Elimina una plantilla existente.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        /// <summary>
        /// Instancia una nueva Solicitud a partir de una plantilla.
        /// </summary>
        [HttpPost("instanciar-solicitud")]
        public async Task<ActionResult<SolicitudDto>> InstanciarSolicitud([FromBody] InstanciarSolicitudDesdePlantillaDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                // If the client didn't provide a valid SolicitanteId, derive it from the authenticated user's OID
                if (dto.SolicitanteId <= 0)
                {
                    var oid = User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                              ?? User?.FindFirst("oid")?.Value;
                    if (string.IsNullOrWhiteSpace(oid))
                    {
                        return BadRequest(new { message = "No se pudo determinar el usuario solicitante desde el token." });
                    }

                    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Oid == oid);
                    if (usuario == null)
                    {
                        return BadRequest(new { message = "Usuario solicitante no existe en la base de datos." });
                    }
                    dto.SolicitanteId = usuario.IdUsuario;
                }
                var solicitud = await _service.InstanciarSolicitudAsync(dto);
                return Created($"/api/solicitudes/{solicitud.IdSolicitud}", solicitud.ToDto());
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return BadRequest(new { message = "Error de base de datos al instanciar la solicitud", detail = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error inesperado al instanciar la solicitud", detail = ex.Message });
            }
        }
    }
}
