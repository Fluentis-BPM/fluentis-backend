using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;
using FluentisCore.DTO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using FluentisCore.Models.WorkflowManagement;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.Models.CommentAndNotificationManagement;
using FluentisCore.Models.MetricsAndReportsManagement;
using FluentisCore.Extensions;
using System.Security.Claims;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class PasoSolicitudController : ControllerBase
    {
        private readonly FluentisContext _context;

        public PasoSolicitudController(FluentisContext context)
        {
            _context = context;
        }

        // POST: api/pasosolicitudes
        [HttpPost]
        public async Task<ActionResult<PasoSolicitud>> CreatePasoSolicitud([FromBody] PasoSolicitudCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var flujoActivo = await _context.FlujosActivos.FindAsync(dto.FlujoActivoId);
            if (flujoActivo == null)
            {
                return NotFound("Flujo activo no encontrado.");
            }

            var paso = new PasoSolicitud
            {
                FlujoActivoId = dto.FlujoActivoId,
                ResponsableId = (dto.TipoPaso == TipoPaso.Ejecucion || dto.TipoPaso == TipoPaso.Inicio) ? dto.ResponsableId : null,
                TipoPaso = dto.TipoPaso,
                Estado = dto.Estado,
                Nombre = dto.Nombre,
                PosX = dto.PosX ?? 0,
                PosY = dto.PosY ?? 0,
                ReglaAprobacion = dto.TipoPaso == TipoPaso.Aprobacion ? dto.ReglaAprobacion : null,
                FechaInicio = DateTime.UtcNow
            };

            _context.PasosSolicitud.Add(paso);
            await _context.SaveChangesAsync();

            // Crear ConexionPasoSolicitud si hay origen
            if (dto.PasoOrigenId.HasValue)
            {
                var conexion = new ConexionPasoSolicitud
                {
                    PasoOrigenId = dto.PasoOrigenId.Value,
                    PasoDestinoId = paso.IdPasoSolicitud,
                    EsExcepcion = false
                };
                _context.ConexionesPasoSolicitud.Add(conexion);
                await _context.SaveChangesAsync();

                // Actualizar tipo_flujo del paso origen
                var origen = await _context.PasosSolicitud.FindAsync(dto.PasoOrigenId);
                if (origen != null)
                {
                    origen.TipoFlujo = await GetTipoFlujo(dto.PasoOrigenId.Value);
                    _context.Entry(origen).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }

            // Inicializar RelacionInput para ejecución, inicio y fin
            if ((dto.TipoPaso == TipoPaso.Ejecucion || dto.TipoPaso == TipoPaso.Inicio || dto.TipoPaso == TipoPaso.Fin) && dto.Inputs != null)
            {
                foreach (var inputDto in dto.Inputs)
                {
                    var input = await _context.Inputs.FindAsync(inputDto.InputId);
                    if (input != null)
                    {
                        var relacion = new RelacionInput
                        {
                            InputId = inputDto.InputId,
                            Nombre = inputDto.Nombre,
                            Valor = inputDto.Valor?.RawValue ?? string.Empty,
                            PlaceHolder = inputDto.PlaceHolder ?? string.Empty,
                            Requerido = inputDto.Requerido,
                            PasoSolicitudId = paso.IdPasoSolicitud
                        };
                        _context.RelacionesInput.Add(relacion);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Devolver una representación plana para evitar ciclos de serialización
            var pasoResult = paso.ToFrontendDto();
            return CreatedAtAction(nameof(GetPasoSolicitud), new { id = paso.IdPasoSolicitud }, pasoResult);
        }

        // DELETE: api/pasosolicitud/{id} (eliminar paso)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePasoSolicitud(int id)
        {
            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso == null)
            {
                return NotFound("Paso de solicitud no encontrado.");
            }

            // Cargar colecciones relacionadas
            await _context.Entry(paso).Collection(p => p.RelacionesInput).LoadAsync();
            await _context.Entry(paso).Reference(p => p.RelacionesGrupoAprobacion).LoadAsync();
            await _context.Entry(paso).Collection(p => p.Comentarios).LoadAsync();
            await _context.Entry(paso).Collection(p => p.Excepciones).LoadAsync();

            // Eliminar dependencias para evitar conflictos de FK (ajusta DbSets si tu contexto usa otros nombres)
            if (paso.RelacionesInput?.Any() == true)
                _context.RelacionesInput.RemoveRange(paso.RelacionesInput);

            if (paso.RelacionesGrupoAprobacion != null)
                _context.RelacionesGrupoAprobacion.Remove(paso.RelacionesGrupoAprobacion);

            if (paso.Comentarios?.Any() == true)
                _context.Comentarios.RemoveRange(paso.Comentarios);

            if (paso.Excepciones?.Any() == true)
                _context.Excepciones.RemoveRange(paso.Excepciones);

            _context.PasosSolicitud.Remove(paso);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // PUT: api/pasosolicitudes/{id} (solo campos básicos)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePasoSolicitud(int id, [FromBody] PasoSolicitudUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso == null)
            {
                return NotFound();
            }

            // Validar estado según tipo_paso
            var validStates = new[] { EstadoPasoSolicitud.Pendiente, EstadoPasoSolicitud.Aprobado,
                                    EstadoPasoSolicitud.Rechazado, EstadoPasoSolicitud.Excepcion };
            if (paso.TipoPaso == TipoPaso.Ejecucion)
            {
                validStates = validStates.Concat(new[] { EstadoPasoSolicitud.Entregado }).ToArray();
            }
            if (!validStates.Contains(dto.Estado))
            {
                return BadRequest("Estado no válido para este tipo de paso.");
            }
            paso.Estado = dto.Estado;

            paso.FechaFin = dto.FechaFin ?? paso.FechaFin;
            if (paso.TipoPaso == TipoPaso.Ejecucion && dto.ResponsableId.HasValue)
            {
                paso.ResponsableId = dto.ResponsableId;
            }
            paso.Nombre = dto.Nombre ?? paso.Nombre;
            if (dto.PosX.HasValue && dto.PosY.HasValue)
            {
                paso.PosX = dto.PosX.Value;
                paso.PosY = dto.PosY.Value;
            }

            // Recalcular tipo_flujo si hay cambios en caminos (simplificado, asumiendo actualización externa)
            paso.TipoFlujo = await GetTipoFlujo(id);
            _context.Entry(paso).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PasoSolicitudExists(id))
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

        // POST: api/pasosolicitudes/{id}/inputs (agregar input)
        [HttpPost("{id}/inputs")]
        public async Task<IActionResult> AddInputToPasoSolicitud(int id, [FromBody] RelacionInputCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso == null)
            {
                return NotFound("Paso no encontrado.");
            }

            if (paso.TipoPaso != TipoPaso.Ejecucion)
            {
                return BadRequest("Solo los pasos de tipo 'ejecución' pueden tener inputs.");
            }

            var input = await _context.Inputs.FindAsync(dto.InputId);
            if (input == null)
            {
                return NotFound("Input no encontrado.");
            }

            var relacion = dto.ToModel();
            relacion.PasoSolicitudId = id;
            _context.RelacionesInput.Add(relacion);
            await _context.SaveChangesAsync();

            // Evitar ciclos de serialización devolviendo un DTO plano sin navegaciones cíclicas
            var result = relacion.ToFrontendDto();
            return CreatedAtAction(nameof(GetPasoSolicitud), new { id }, result);
        }

        // PUT: api/pasosolicitudes/{id}/inputs/{inputId} (editar input)
        [HttpPut("{id}/inputs/{inputId}")]
        public async Task<IActionResult> UpdateInputOfPasoSolicitud(int id, int inputId, [FromBody] RelacionInputUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var relacion = await _context.RelacionesInput
                .FirstOrDefaultAsync(r => r.PasoSolicitudId == id && r.IdRelacion == inputId);
            if (relacion == null)
            {
                return NotFound("Relación de input no encontrada.");
            }

            // Reutilizar extensión para mantener consistencia, incluida OptionsJson
            relacion.UpdateFromDto(dto);

            _context.Entry(relacion).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/pasosolicitudes/{id}/inputs/{inputId} (quitar input)
        [HttpDelete("{id}/inputs/{inputId}")]
        public async Task<IActionResult> RemoveInputFromPasoSolicitud(int id, int inputId)
        {
            var relacion = await _context.RelacionesInput
                .FirstOrDefaultAsync(r => r.PasoSolicitudId == id && r.IdRelacion == inputId);
            if (relacion == null)
            {
                return NotFound("Relación de input no encontrada.");
            }

            _context.RelacionesInput.Remove(relacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        // POST: api/pasosolicitudes/{id}/grupoaprobacion (crear relación de grupo de aprobación)
        [HttpPost("{id}/grupoaprobacion")]
        public async Task<ActionResult<RelacionGrupoAprobacionDto>> CrearRelacionPasoGrupoAprobacion(int id, [FromBody] RelacionGrupoAprobacionCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso == null)
            {
                return NotFound("Paso no encontrado.");
            }

            if (paso.TipoPaso != TipoPaso.Aprobacion)
            {
                return BadRequest("Solo los pasos de tipo 'aprobación' pueden tener un grupo de aprobación.");
            }

            var grupo = await _context.GruposAprobacion.FindAsync(dto.GrupoAprobacionId);
            if (grupo == null)
            {
                return NotFound("Grupo de aprobación no encontrado.");
            }

            var existeRelacion = await _context.RelacionesGrupoAprobacion
                .AnyAsync(r => r.PasoSolicitudId == id);
            if (existeRelacion)
            {
                return Conflict("El paso ya tiene un grupo de aprobación asociado. Use PUT para actualizarlo.");
            }

            var relacion = new RelacionGrupoAprobacion
            {
                GrupoAprobacionId = dto.GrupoAprobacionId,
                PasoSolicitudId = id
            };

            _context.RelacionesGrupoAprobacion.Add(relacion);
            await _context.SaveChangesAsync();

            // Map to DTO and return 201 pointing to the paso resource
            var resultDto = relacion.ToDto();
            return CreatedAtAction(nameof(GetPasoSolicitud), new { id }, resultDto);
        }

        // DELETE: api/pasosolicitudes/{id}/grupoaprobacion (eliminar relación de grupo de aprobación)
        [HttpDelete("{id}/grupoaprobacion")]
        public async Task<IActionResult> EliminarRelacionPasoGrupoAprobacion(int id)
        {
            var paso = await _context.PasosSolicitud
                .Include(p => p.RelacionesGrupoAprobacion)
                .ThenInclude(r => r.Decisiones)
                .FirstOrDefaultAsync(p => p.IdPasoSolicitud == id);

            if (paso == null)
            {
                return NotFound("Paso no encontrado.");
            }

            var relacion = paso.RelacionesGrupoAprobacion;
            if (relacion == null)
            {
                return NoContent(); // Idempotente: nada que eliminar
            }

            // Eliminar decisiones asociadas para evitar conflictos de FK
            if (relacion.Decisiones?.Any() == true)
            {
                _context.DecisionesUsuario.RemoveRange(relacion.Decisiones);
            }

            _context.RelacionesGrupoAprobacion.Remove(relacion);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/pasosolicitudes/{id}/comentarios (agregar comentario)
        [HttpPost("{id}/comentarios")]
        public async Task<IActionResult> AddComentarioToPasoSolicitud(int id, [FromBody] ComentarioCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso == null)
            {
                return NotFound("Paso no encontrado.");
            }

            var comentario = new Comentario
            {
                PasoSolicitudId = id,
                UsuarioId = dto.UsuarioId,
                Contenido = dto.Contenido,
                Fecha = DateTime.UtcNow
            };
            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

            var pasoDto = paso.ToFrontendDto();
            return CreatedAtAction(nameof(GetPasoSolicitud), new { id }, pasoDto);
        }

        // DELETE: api/pasosolicitudes/{id}/comentarios/{comentarioId} (quitar comentario)
        [HttpDelete("{id}/comentarios/{comentarioId}")]
        public async Task<IActionResult> RemoveComentarioFromPasoSolicitud(int id, int comentarioId)
        {
            var comentario = await _context.Comentarios
                .FirstOrDefaultAsync(c => c.PasoSolicitudId == id && c.IdComentario == comentarioId);
            if (comentario == null)
            {
                return NotFound("Comentario no encontrado.");
            }

            _context.Comentarios.Remove(comentario);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/pasosolicitudes/{id}/excepciones (agregar excepción)
        [HttpPost("{id}/excepciones")]
        public async Task<IActionResult> AddExcepcionToPasoSolicitud(int id, [FromBody] ExcepcionCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso == null)
            {
                return NotFound("Paso no encontrado.");
            }

            var excepcion = new Excepcion
            {
                PasoSolicitudId = id,
                UsuarioId = dto.UsuarioId,
                Motivo = dto.Motivo,
                FechaRegistro = DateTime.UtcNow,
            };
            _context.Excepciones.Add(excepcion);
            await _context.SaveChangesAsync();

            // Cambiar estado del paso a "Excepcion" si no lo estaba
            if (paso.Estado != EstadoPasoSolicitud.Excepcion)
            {
                paso.Estado = EstadoPasoSolicitud.Excepcion;
                _context.Entry(paso).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            var pasoDto2 = paso.ToFrontendDto();
            return CreatedAtAction(nameof(GetPasoSolicitud), new { id }, pasoDto2);
        }

        // DELETE: api/pasosolicitudes/{id}/excepciones/{excepcionId} (quitar excepción)
        [HttpDelete("{id}/excepciones/{excepcionId}")]
        public async Task<IActionResult> RemoveExcepcionFromPasoSolicitud(int id, int excepcionId)
        {
            var excepcion = await _context.Excepciones
                .FirstOrDefaultAsync(e => e.PasoSolicitudId == id && e.IdExcepcion == excepcionId);
            if (excepcion == null)
            {
                return NotFound("Excepción no encontrada.");
            }

            _context.Excepciones.Remove(excepcion);
            await _context.SaveChangesAsync();

            // Revisar si quedan excepciones; si no, restaurar estado anterior (simplificado)
            var excepcionesRestantes = await _context.Excepciones
                .Where(e => e.PasoSolicitudId == id).ToListAsync();
            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso != null && excepcionesRestantes.Count == 0 && paso.Estado == EstadoPasoSolicitud.Excepcion)
            {
                paso.Estado = EstadoPasoSolicitud.Pendiente; // Restaurar a pendiente como ejemplo
                _context.Entry(paso).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // POST: api/pasosolicitudes/{id}/decisiones (agregar decisión)
        [HttpPost("{id}/decisiones")]
        public async Task<IActionResult> AddDecisionToPasoSolicitud(int id, [FromBody] RelacionDecisionUsuarioCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var paso = await _context.PasosSolicitud
                .Include(p => p.RelacionesGrupoAprobacion)
                .ThenInclude(r => r.GrupoAprobacion)
                .ThenInclude(g => g.RelacionesUsuarioGrupo)
                .FirstOrDefaultAsync(p => p.IdPasoSolicitud == id);
            if (paso == null)
            {
                return NotFound("Paso no encontrado.");
            }

            if (paso.TipoPaso != TipoPaso.Aprobacion)
            {
                return BadRequest("Solo los pasos de tipo 'aprobación' pueden tener decisiones.");
            }

            var relacionGrupo = paso.RelacionesGrupoAprobacion;
            if (relacionGrupo == null || relacionGrupo.GrupoAprobacion == null)
            {
                return BadRequest("No hay grupo de aprobación asociado.");
            }

            var decision = new RelacionDecisionUsuario
            {
                RelacionGrupoAprobacionId = relacionGrupo.IdRelacion,
                IdUsuario = dto.IdUsuario,
                Decision = dto.Decision,
                FechaDecision = DateTime.UtcNow
            };
            _context.DecisionesUsuario.Add(decision);
            await _context.SaveChangesAsync();

            // Actualizar estado según regla de votación
            await UpdateEstadoPorVotacion(paso.IdPasoSolicitud);

            var pasoDto3 = paso.ToFrontendDto();
            return CreatedAtAction(nameof(GetPasoSolicitud), new { id }, pasoDto3);
        }

        // DELETE: api/pasosolicitudes/{id}/decisiones/{decisionId} (quitar decisión)
        [HttpDelete("{id}/decisiones/{decisionId}")]
        public async Task<IActionResult> RemoveDecisionFromPasoSolicitud(int id, int decisionId)
        {
            var decision = await _context.DecisionesUsuario
                .FirstOrDefaultAsync(d => d.RelacionGrupoAprobacion.PasoSolicitudId == id && d.IdRelacion == decisionId);
            if (decision == null)
            {
                return NotFound("Decisión no encontrada.");
            }

            _context.DecisionesUsuario.Remove(decision);
            await _context.SaveChangesAsync();

            // Actualizar estado según regla de votación
            var paso = await _context.PasosSolicitud.FindAsync(id);
            await UpdateEstadoPorVotacion(id);

            return NoContent();
        }

        // DELETE: api/pasosolicitudes/{id}/conexiones/{destinoId} (eliminar conexión)
        [HttpDelete("{id}/conexiones/{destinoId}")]
        public async Task<IActionResult> RemoveConnectionFromPasoSolicitud(int id, int destinoId)
        {
            var conexion = await _context.ConexionesPasoSolicitud
                .FirstOrDefaultAsync(c => c.PasoOrigenId == id && c.PasoDestinoId == destinoId);
            if (conexion == null)
            {
                return NotFound("Conexión no encontrada.");
            }

            _context.ConexionesPasoSolicitud.Remove(conexion);
            await _context.SaveChangesAsync();

            // Actualizar tipo_flujo del paso origen
            var origen = await _context.PasosSolicitud.FindAsync(id);
            if (origen != null)
            {
                origen.TipoFlujo = await GetTipoFlujo(id);
                _context.Entry(origen).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // POST: api/pasosolicitudes/{id}/conexiones (agregar UNA conexión sin reemplazar las existentes)


        [HttpPost("{id}/conexiones")]
        public async Task<IActionResult> AddConnectionToPasoSolicitud(int id, [FromBody] ConexionCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var origen = await _context.PasosSolicitud.FindAsync(id);
            if (origen == null)
            {
                return NotFound("Paso origen no encontrado.");
            }

            var destino = await _context.PasosSolicitud.FindAsync(dto.DestinoId);
            if (destino == null)
            {
                return NotFound("Paso destino no encontrado.");
            }

            if (destino.FlujoActivoId != origen.FlujoActivoId)
            {
                return BadRequest("El paso destino no pertenece al mismo flujo activo.");
            }

            var existente = await _context.ConexionesPasoSolicitud
                .AnyAsync(c => c.PasoOrigenId == id && c.PasoDestinoId == dto.DestinoId);
            if (existente)
            {
                // Idempotente: si ya existe, no hacemos nada y devolvemos 204
                return NoContent();
            }

            var nuevaConexion = new ConexionPasoSolicitud
            {
                PasoOrigenId = id,
                PasoDestinoId = dto.DestinoId,
                EsExcepcion = dto.EsExcepcion
            };

            _context.ConexionesPasoSolicitud.Add(nuevaConexion);
            await _context.SaveChangesAsync();

            // Actualizar tipo_flujo del paso origen
            origen.TipoFlujo = await GetTipoFlujo(id);
            _context.Entry(origen).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPasoSolicitud), new { id }, origen);
        }

        // PUT: api/pasosolicitudes/{id}/conexiones (cambiar todas las conexiones)
        [HttpPut("{id}/conexiones")]
        public async Task<IActionResult> UpdateConnectionsOfPasoSolicitud(int id, [FromBody] List<int> nuevosDestinos)
        {
            if (nuevosDestinos == null || !nuevosDestinos.Any())
            {
                return BadRequest("Debe proporcionar al menos un destino.");
            }

            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso == null)
            {
                return NotFound("Paso no encontrado.");
            }

            // Eliminar conexiones existentes
            var conexionesExistentes = await _context.ConexionesPasoSolicitud
                .Where(c => c.PasoOrigenId == id).ToListAsync();
            _context.ConexionesPasoSolicitud.RemoveRange(conexionesExistentes);

            // Crear nuevas conexiones
            foreach (var destinoId in nuevosDestinos)
            {
                var destino = await _context.PasosSolicitud.FindAsync(destinoId);
                if (destino != null && destino.FlujoActivoId == paso.FlujoActivoId)
                {
                    var nuevaConexion = new ConexionPasoSolicitud
                    {
                        PasoOrigenId = id,
                        PasoDestinoId = destinoId,
                        EsExcepcion = false
                    };
                    _context.ConexionesPasoSolicitud.Add(nuevaConexion);
                }
                else
                {
                    return BadRequest($"Destino con ID {destinoId} no encontrado o no pertenece al mismo flujo.");
                }
            }

            await _context.SaveChangesAsync();

            // Actualizar tipo_flujo del paso origen
            paso.TipoFlujo = await GetTipoFlujo(id);
            _context.Entry(paso).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private async Task UpdateEstadoPorVotacion(int pasoId)
        {
            var paso = await _context.PasosSolicitud
                .Include(p => p.RelacionesGrupoAprobacion)
                .ThenInclude(r => r.GrupoAprobacion)
                .ThenInclude(g => g.RelacionesUsuarioGrupo)
                .Include(p => p.RelacionesGrupoAprobacion)
                .ThenInclude(r => r.Decisiones)
                .FirstOrDefaultAsync(p => p.IdPasoSolicitud == pasoId);

            if (paso == null || paso.TipoPaso != TipoPaso.Aprobacion || paso.RelacionesGrupoAprobacion == null)
                return;

            var grupo = paso.RelacionesGrupoAprobacion.GrupoAprobacion;
            var decisiones = paso.RelacionesGrupoAprobacion.Decisiones;
            var totalUsuarios = grupo.RelacionesUsuarioGrupo.Count;
            var aprobaciones = decisiones.Count(d => d.Decision == true);
            var rechazos = decisiones.Count(d => d.Decision == false);

            switch (paso.ReglaAprobacion)
            {
                case ReglaAprobacion.Unanimidad:
                    if (aprobaciones == totalUsuarios && rechazos == 0)
                        paso.Estado = EstadoPasoSolicitud.Aprobado;
                    else if (rechazos > 0)
                        paso.Estado = EstadoPasoSolicitud.Rechazado;
                    break;

                case ReglaAprobacion.PrimeraAprobacion:
                    if (aprobaciones > 0)
                        paso.Estado = EstadoPasoSolicitud.Aprobado;
                    break;

                case ReglaAprobacion.Mayoria:
                    if (aprobaciones > totalUsuarios / 2)
                        paso.Estado = EstadoPasoSolicitud.Aprobado;
                    else if (rechazos > totalUsuarios / 2)
                        paso.Estado = EstadoPasoSolicitud.Rechazado;
                    break;
            }

            _context.Entry(paso).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        private async Task<TipoFlujo> GetTipoFlujo(int pasoId)
        {
            var destinos = await _context.ConexionesPasoSolicitud
                .Where(c => c.PasoOrigenId == pasoId)
                .Select(c => c.PasoDestinoId)
                .ToListAsync();
            var origenes = await _context.ConexionesPasoSolicitud
                .Where(c => c.PasoDestinoId == pasoId)
                .Select(c => c.PasoOrigenId)
                .ToListAsync();

            if (destinos.Count > 1) return TipoFlujo.Bifurcacion;
            if (origenes.Count > 1) return TipoFlujo.Union;
            return TipoFlujo.Normal;
        }

        private bool PasoSolicitudExists(int id)
        {
            return _context.PasosSolicitud.Any(e => e.IdPasoSolicitud == id);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PasoSolicitudFrontendDto>> GetPasoSolicitud(int id)
        {
            // Incluir relaciones necesarias para armar el DTO plano
            var paso = await _context.PasosSolicitud
                .Include(p => p.RelacionesInput)
                    .ThenInclude(ri => ri.Input) // Necesario para que la navegación Input no sea null (sin lazy loading)
                .Include(p => p.RelacionesGrupoAprobacion)
                    .ThenInclude(r => r.GrupoAprobacion)
                        .ThenInclude(g => g.RelacionesUsuarioGrupo)
                .Include(p => p.Comentarios)
                .Include(p => p.Excepciones)
                .FirstOrDefaultAsync(p => p.IdPasoSolicitud == id);

            if (paso == null)
            {
                return NotFound();
            }

            return paso.ToFrontendDto();
        }

        // GET: api/pasosolicitudes/{id}/inputs (obtener todos los inputs asociados a un paso)
        [HttpGet("{id}/inputs")]
        public async Task<ActionResult<IEnumerable<RelacionInputFrontendDto>>> GetInputsForPaso(int id, [FromQuery] bool debug = false)
        {
            var existePaso = await _context.PasosSolicitud.AnyAsync(p => p.IdPasoSolicitud == id);
            if (!existePaso)
            {
                return NotFound("Paso no encontrado.");
            }

            // Explicit join to ensure we get the TipoInput information
            var query = from r in _context.RelacionesInput
                        join i in _context.Inputs on r.InputId equals i.IdInput
                        where r.PasoSolicitudId == id
                        select new { Relacion = r, Input = i };

            var materialized = await query.ToListAsync();

            if (debug)
            {
                Console.WriteLine($"[DEBUG] PasoSolicitud {id} -> {materialized.Count} inputs encontrados");
                foreach (var item in materialized)
                {
                    Console.WriteLine($"[DEBUG] RelacionInput Id={item.Relacion.IdRelacion} InputId={item.Relacion.InputId} EnumValue={(int)item.Input.TipoInput} EnumName={item.Input.TipoInput} Mapped={MapTipoInputController(item.Input.TipoInput)}");
                }
            }

            var inputsWithTypes = materialized.Select(item => new RelacionInputFrontendDto
            {
                IdRelacion = item.Relacion.IdRelacion,
                InputId = item.Relacion.InputId,
                Nombre = item.Relacion.Nombre,
                PlaceHolder = item.Relacion.PlaceHolder,
                Requerido = item.Relacion.Requerido,
                Valor = item.Relacion.Valor,
                TipoInput = MapTipoInputController(item.Input.TipoInput),
                PasoSolicitudId = item.Relacion.PasoSolicitudId,
                SolicitudId = item.Relacion.SolicitudId
            }).ToList();

            return inputsWithTypes;
        }

        private string MapTipoInputController(TipoInput tipo)
            => tipo switch
            {
                TipoInput.TextoCorto => "texto_corto",
                TipoInput.TextoLargo => "texto_largo", 
                TipoInput.Combobox => "combobox",
                TipoInput.MultipleCheckbox => "multiple_checkbox",
                TipoInput.RadioGroup => "radiogroup",
                TipoInput.Date => "date",
                TipoInput.Number => "number", 
                TipoInput.Archivo => "archivo",
                _ => "texto_corto"
            };

        // GET: api/pasosolicitudes/usuario/{usuarioId} (obtener pasos por usuario)
        [HttpGet("usuario/{usuarioId}")]
        public async Task<ActionResult<IEnumerable<PasoSolicitudFrontendDto>>> GetPasosByUsuario(int usuarioId, [FromQuery] string? tipoPaso = null, [FromQuery] EstadoPasoSolicitud? estado = null, [FromQuery] DateTime? fechaDesde = null, [FromQuery] DateTime? fechaHasta = null)
        {
            // Obtener OID del usuario actual desde el token JWT (claim estándar de Azure AD)
            var oidClaim = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (oidClaim == null)
            {
                return Unauthorized("Token inválido: OID no encontrado.");
            }
            var oid = oidClaim.Value;

            // Buscar el usuario en la DB usando OID para obtener IdUsuario
            var currentUser = await _context.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Oid == oid);
            if (currentUser == null)
            {
                return Unauthorized("Usuario no encontrado en la base de datos.");
            }
            var currentUserId = currentUser.IdUsuario;
            bool isAdmin = currentUser.Rol?.Nombre == "Administrador";

            if (!isAdmin && currentUserId != usuarioId)
            {
                return Forbid("No tienes permiso para ver pasos de otros usuarios.");
            }

            // Resto del código permanece igual...
            // Query base: pasos de tipo Ejecución o Aprobación
            var query = _context.PasosSolicitud
                .Where(p => p.TipoPaso == TipoPaso.Ejecucion || p.TipoPaso == TipoPaso.Aprobacion)
                .Include(p => p.FlujoActivo)
                .Include(p => p.RelacionesInput)
                .Include(p => p.RelacionesGrupoAprobacion)
                    .ThenInclude(rga => rga.GrupoAprobacion)
                        .ThenInclude(ga => ga.RelacionesUsuarioGrupo)
                .Include(p => p.Comentarios)
                .Include(p => p.Excepciones)
                .AsQueryable();

            // Filtrar por asignación al usuario
            query = query.Where(p =>
                (p.TipoPaso == TipoPaso.Ejecucion && p.ResponsableId == usuarioId) ||
                (p.TipoPaso == TipoPaso.Aprobacion &&
                 p.RelacionesGrupoAprobacion != null &&
                 p.RelacionesGrupoAprobacion.GrupoAprobacion.RelacionesUsuarioGrupo.Any(rug => rug.UsuarioId == usuarioId))
            );

            // Filtros opcionales (tipoPaso, estado, fechas)
            if (!string.IsNullOrEmpty(tipoPaso))
            {
                if (tipoPaso.ToLower() == "ejecucion")
                    query = query.Where(p => p.TipoPaso == TipoPaso.Ejecucion);
                else if (tipoPaso.ToLower() == "aprobacion")
                    query = query.Where(p => p.TipoPaso == TipoPaso.Aprobacion);
                else
                    return BadRequest("TipoPaso inválido. Usa 'ejecucion' o 'aprobacion'.");
            }

            if (estado.HasValue)
            {
                query = query.Where(p => p.Estado == estado.Value);
            }

            if (fechaDesde.HasValue)
            {
                query = query.Where(p => p.FechaInicio >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                query = query.Where(p => p.FechaInicio <= fechaHasta.Value);
            }

            // Ordenar por fecha de inicio descendente
            query = query.OrderByDescending(p => p.FechaInicio);

            var pasos = await query.ToListAsync();
            return pasos.Select(p => p.ToFrontendDto()).ToList();
        }
    }
}