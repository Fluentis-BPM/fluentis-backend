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
using FluentisCore.Services;

namespace FluentisCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequireAccessAsUser")]
    public class PasoSolicitudController : ControllerBase
    {
        private readonly FluentisContext _context;
        private readonly WorkflowInitializationService _workflowService;
        private readonly NotificationService _notificationService;
        private readonly WorkflowResetService _resetService;

        public PasoSolicitudController(
            FluentisContext context, 
            WorkflowInitializationService workflowService, 
            NotificationService notificationService,
            WorkflowResetService resetService)
        {
            _context = context;
            _workflowService = workflowService;
            _notificationService = notificationService;
            _resetService = resetService;
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

            // Notificaciones según tipo de paso
            try
            {
                var solicitud = await _context.FlujosActivos
                    .Where(f => f.IdFlujoActivo == dto.FlujoActivoId)
                    .Select(f => new { f.Nombre })
                    .FirstOrDefaultAsync();

                if (dto.TipoPaso == TipoPaso.Ejecucion && dto.ResponsableId.HasValue)
                {
                    // Notificar asignación de paso de ejecución
                    await _notificationService.NotificarAsignacionPasoAsync(
                        dto.ResponsableId.Value,
                        paso.IdPasoSolicitud,
                        dto.Nombre,
                        solicitud?.Nombre ?? ""
                    );
                }
            }
            catch (Exception ex)
            {
                // No fallar la creación del paso si falla la notificación
                Console.WriteLine($"Error al enviar notificación: {ex.Message}");
            }

            // Devolver una representación plana para evitar ciclos de serialización
            var pasoResult = paso.ToFrontendDto();
            return CreatedAtAction(nameof(GetPasoSolicitud), new { id = paso.IdPasoSolicitud }, pasoResult);
        }

        // DELETE: api/pasosolicitud/{id} (eliminar paso)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePasoSolicitud(int id)
        {
            // Buscar el paso
            var paso = await _context.PasosSolicitud.FindAsync(id);
            if (paso == null)
            {
                return NotFound("Paso de solicitud no encontrado.");
            }

            // Ejecutar todo en una transacción resiliente (compatible con SqlServerRetryingExecutionStrategy)
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1) Eliminar conexiones entrantes y salientes (DeleteBehavior.NoAction requiere borrarlas manualmente)
                    var conexionesSalientes = await _context.ConexionesPasoSolicitud
                        .Where(c => c.PasoOrigenId == id)
                        .ToListAsync();
                    if (conexionesSalientes.Count > 0)
                    {
                        _context.ConexionesPasoSolicitud.RemoveRange(conexionesSalientes);
                        await _context.SaveChangesAsync();
                    }

                    var conexionesEntrantes = await _context.ConexionesPasoSolicitud
                        .Where(c => c.PasoDestinoId == id)
                        .ToListAsync();
                    if (conexionesEntrantes.Count > 0)
                    {
                        _context.ConexionesPasoSolicitud.RemoveRange(conexionesEntrantes);
                        await _context.SaveChangesAsync();
                    }

                    // 2) Eliminar decisiones y relación de grupo de aprobación si existe (Restrict exige borrar decisiones primero)
                    var relacionGrupo = await _context.RelacionesGrupoAprobacion
                        .Include(r => r.Decisiones)
                        .FirstOrDefaultAsync(r => r.PasoSolicitudId == id);
                    if (relacionGrupo != null)
                    {
                        if (relacionGrupo.Decisiones?.Any() == true)
                        {
                            _context.DecisionesUsuario.RemoveRange(relacionGrupo.Decisiones);
                            await _context.SaveChangesAsync();
                        }
                        _context.RelacionesGrupoAprobacion.Remove(relacionGrupo);
                        await _context.SaveChangesAsync();
                    }

                    // 3) Cargar y eliminar colecciones relacionadas que cuelgan del paso (inputs, comentarios, excepciones)
                    await _context.Entry(paso).Collection(p => p.RelacionesInput).LoadAsync();
                    await _context.Entry(paso).Collection(p => p.Comentarios).LoadAsync();
                    await _context.Entry(paso).Collection(p => p.Excepciones).LoadAsync();

                    if (paso.RelacionesInput?.Any() == true)
                        _context.RelacionesInput.RemoveRange(paso.RelacionesInput);

                    if (paso.Comentarios?.Any() == true)
                        _context.Comentarios.RemoveRange(paso.Comentarios);

                    if (paso.Excepciones?.Any() == true)
                        _context.Excepciones.RemoveRange(paso.Excepciones);

                    await _context.SaveChangesAsync();

                    // 4) Eliminar el paso
                    _context.PasosSolicitud.Remove(paso);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

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

            var estadoAnterior = paso.Estado;

            // Validar y actualizar estado solo si viene en el DTO (evita resetear a Pendiente en PUT parciales)
            if (dto.Estado.HasValue)
            {
                var validStates = new[]
                {
                    EstadoPasoSolicitud.Pendiente,
                    EstadoPasoSolicitud.Aprobado,
                    EstadoPasoSolicitud.Rechazado,
                    EstadoPasoSolicitud.Excepcion
                };
                if (paso.TipoPaso == TipoPaso.Ejecucion)
                {
                    validStates = validStates.Concat(new[] { EstadoPasoSolicitud.Entregado }).ToArray();
                }
                if (!validStates.Contains(dto.Estado.Value))
                {
                    return BadRequest("Estado no válido para este tipo de paso.");
                }

                paso.Estado = dto.Estado.Value;
            }

            var responsableAnterior = paso.ResponsableId;
            
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

                // ✅ NUEVO: Intentar avanzar el flujo cuando cambia el estado
                if (dto.Estado.HasValue && estadoAnterior != dto.Estado.Value)
                {
                    Console.WriteLine($"🔄 Estado cambió de {estadoAnterior} a {dto.Estado.Value}. Intentando avanzar flujo...");
                    await TryAdvanceFromPasoAsync(paso, estadoAnterior);
                }

                // Notificar cambio de estado si hubo uno
                if (dto.Estado.HasValue && estadoAnterior != dto.Estado.Value && paso.ResponsableId.HasValue)
                {
                    try
                    {
                        await _notificationService.NotificarCambioEstadoPasoAsync(
                            paso.ResponsableId.Value,
                            paso.Nombre,
                            estadoAnterior.ToString(),
                            dto.Estado.Value.ToString()
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al notificar cambio de estado: {ex.Message}");
                    }
                }

                // Notificar cambio de responsable si hubo uno
                if (dto.ResponsableId.HasValue && responsableAnterior.HasValue && 
                    responsableAnterior.Value != dto.ResponsableId.Value)
                {
                    try
                    {
                        await _notificationService.NotificarCambioResponsableAsync(
                            responsableAnterior.Value,
                            dto.ResponsableId.Value,
                            paso.Nombre
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al notificar cambio de responsable: {ex.Message}");
                    }
                }

                // Verificar si el paso se completó (Aprobado o Entregado) y si debe finalizar el flujo
                if (dto.Estado.HasValue &&
                    (dto.Estado.Value == EstadoPasoSolicitud.Aprobado || dto.Estado.Value == EstadoPasoSolicitud.Entregado))
                {
                    await _workflowService.VerificarYFinalizarFlujoAsync(id);
                }
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

            // Notificar a todos los usuarios del grupo de aprobación
            try
            {
                var solicitud = await _context.FlujosActivos
                    .Where(f => f.IdFlujoActivo == paso.FlujoActivoId)
                    .Select(f => new { f.Nombre })
                    .FirstOrDefaultAsync();

                await _notificationService.NotificarGrupoAprobacionAsync(
                    dto.GrupoAprobacionId,
                    $"Se requiere tu aprobación para el paso '{paso.Nombre}' en '{solicitud?.Nombre ?? "solicitud"}'",
                    PrioridadNotificacion.Alta
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar notificación a grupo: {ex.Message}");
            }

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

            // Notificar al responsable del paso si no es quien comenta
            try
            {
                if (paso.ResponsableId.HasValue && paso.ResponsableId.Value != dto.UsuarioId)
                {
                    var usuario = await _context.Usuarios
                        .Where(u => u.IdUsuario == dto.UsuarioId)
                        .Select(u => u.Nombre)
                        .FirstOrDefaultAsync();

                    await _notificationService.NotificarComentarioAgregadoAsync(
                        paso.ResponsableId.Value,
                        paso.Nombre,
                        usuario ?? "Un usuario"
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar notificación de comentario: {ex.Message}");
            }

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

            // Notificar excepción crítica al responsable y supervisores
            try
            {
                var usuariosANotificar = new List<int>();
                
                // Agregar responsable del paso si existe
                if (paso.ResponsableId.HasValue)
                {
                    usuariosANotificar.Add(paso.ResponsableId.Value);
                }

                // Aquí podrías agregar lógica para notificar a supervisores/administradores
                // Por ahora solo notificamos al responsable
                
                if (usuariosANotificar.Any())
                {
                    await _notificationService.NotificarExcepcionPasoAsync(
                        usuariosANotificar,
                        paso.Nombre,
                        dto.Motivo
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar notificación de excepción: {ex.Message}");
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

            // Ejecutar toda la operación en una transacción resiliente para consistencia
            var strategy = _context.Database.CreateExecutionStrategy();
            PasoSolicitud pasoActualizado;
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Upsert de decisión (idempotente)
                    var existing = await _context.DecisionesUsuario
                        .FirstOrDefaultAsync(d => d.RelacionGrupoAprobacionId == relacionGrupo.IdRelacion && d.IdUsuario == dto.IdUsuario);

                    if (existing != null)
                    {
                        existing.Decision = dto.Decision;
                        existing.FechaDecision = DateTime.UtcNow;
                        _context.Entry(existing).State = EntityState.Modified;
                    }
                    else
                    {
                        var decision = new RelacionDecisionUsuario
                        {
                            RelacionGrupoAprobacionId = relacionGrupo.IdRelacion,
                            IdUsuario = dto.IdUsuario,
                            Decision = dto.Decision,
                            FechaDecision = DateTime.UtcNow
                        };
                        _context.DecisionesUsuario.Add(decision);
                    }
                    await _context.SaveChangesAsync();

                    await UpdateEstadoPorVotacion(id);
                    

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });

            // Cargar estado fresco para responder
            pasoActualizado = await _context.PasosSolicitud
                .Include(p => p.RelacionesInput)
                .Include(p => p.RelacionesGrupoAprobacion)
                .Include(p => p.Comentarios)
                .Include(p => p.Excepciones)
                .FirstOrDefaultAsync(p => p.IdPasoSolicitud == id) ?? paso;

            // Notificar decisión de aprobación/rechazo
            try
            {
                var flujo = await _context.FlujosActivos
                    .Where(f => f.IdFlujoActivo == pasoActualizado.FlujoActivoId)
                    .FirstOrDefaultAsync();
                
                var usuario = await _context.Usuarios
                    .Where(u => u.IdUsuario == dto.IdUsuario)
                    .Select(u => u.Nombre)
                    .FirstOrDefaultAsync();

                // Notificar al creador de la solicitud (si existe el concepto)
                // Por ahora notificamos al responsable del paso si existe
                if (pasoActualizado.ResponsableId.HasValue && pasoActualizado.ResponsableId.Value != dto.IdUsuario)
                {
                    await _notificationService.NotificarDecisionAprobacionAsync(
                        pasoActualizado.ResponsableId.Value,
                        pasoActualizado.Nombre,
                        usuario ?? "Un aprobador",
                        dto.Decision,
                        ""
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar notificación de decisión: {ex.Message}");
            }

            var pasoDto3 = pasoActualizado.ToFrontendDto();
            return Ok(pasoDto3);
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

            // ✅ NUEVA VALIDACIÓN: Caminos de excepción solo pueden originarse desde pasos de Aprobación
            if (dto.EsExcepcion && origen.TipoPaso != TipoPaso.Aprobacion)
            {
                return BadRequest("Los caminos de excepción solo pueden originarse desde pasos de tipo Aprobación.");
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

            var estadoAnterior = paso.Estado;
            var grupo = paso.RelacionesGrupoAprobacion.GrupoAprobacion;
            var decisiones = paso.RelacionesGrupoAprobacion.Decisiones ?? new List<RelacionDecisionUsuario>();
            var totalUsuarios = grupo.RelacionesUsuarioGrupo.Count;

            // Consolidar por usuario (última decisión por usuario prevalece) para evitar duplicados
            var decisionesPorUsuario = decisiones
                .GroupBy(d => d.IdUsuario)
                .Select(g => g.OrderByDescending(x => x.FechaDecision).First())
                .ToList();

            var aprobaciones = decisionesPorUsuario.Count(d => d.Decision == true);
            var rechazos = decisionesPorUsuario.Count(d => d.Decision == false);

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

            // ✅ NUEVO: Intentar avanzar el flujo cuando cambia el estado por votación
            if (estadoAnterior != paso.Estado)
            {
                Console.WriteLine($"🗳️  Estado cambió por votación de {estadoAnterior} a {paso.Estado}. Intentando avanzar flujo...");
                await TryAdvanceFromPasoAsync(paso, estadoAnterior);
            }

            // Notificar cambio de estado por votación si cambió
            if (estadoAnterior != paso.Estado)
            {
                try
                {
                    // Notificar a todos los miembros del grupo sobre el resultado
                    var usuariosGrupo = grupo.RelacionesUsuarioGrupo
                        .Select(r => r.UsuarioId)
                        .ToList();

                    var mensaje = paso.Estado == EstadoPasoSolicitud.Aprobado
                        ? $"✅ El paso '{paso.Nombre}' ha sido APROBADO por votación"
                        : $"❌ El paso '{paso.Nombre}' ha sido RECHAZADO por votación";

                    await _notificationService.CrearNotificacionesMasivasAsync(
                        usuariosGrupo,
                        mensaje,
                        PrioridadNotificacion.Alta
                    );

                    // Notificar al responsable del paso si existe
                    if (paso.ResponsableId.HasValue && !usuariosGrupo.Contains(paso.ResponsableId.Value))
                    {
                        await _notificationService.NotificarCambioEstadoPasoAsync(
                            paso.ResponsableId.Value,
                            paso.Nombre,
                            estadoAnterior.ToString(),
                            paso.Estado.ToString()
                        );
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al notificar cambio de estado por votación: {ex.Message}");
                }
            }

            // Si el paso se aprobó, verificar si el flujo debe finalizarse
            if (estadoAnterior != EstadoPasoSolicitud.Aprobado && paso.Estado == EstadoPasoSolicitud.Aprobado)
            {
                await _workflowService.VerificarYFinalizarFlujoAsync(pasoId);
            }
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

        // --- Encadenamiento de flujo: helpers privados ---
        private static bool EsExito(EstadoPasoSolicitud estado)
            => estado == EstadoPasoSolicitud.Aprobado || estado == EstadoPasoSolicitud.Entregado;

        private static bool EsFallo(EstadoPasoSolicitud estado)
            => estado == EstadoPasoSolicitud.Rechazado || estado == EstadoPasoSolicitud.Excepcion;

        private async Task TryAdvanceFromPasoAsync(PasoSolicitud paso, EstadoPasoSolicitud estadoAnterior)
        {
            // Avanzar solo cuando hay transición a un estado terminal de interés
            if (paso == null) return;
            if (paso.Estado == estadoAnterior) return;

            var avanzarPorExcepcion = EsFallo(paso.Estado);
            var avanzarPorExito = EsExito(paso.Estado);
            if (!avanzarPorExito && !avanzarPorExcepcion) return;

            // Buscar conexiones salientes según resultado (normal vs excepción)
            var conexiones = await _context.ConexionesPasoSolicitud
                .Where(c => c.PasoOrigenId == paso.IdPasoSolicitud && c.EsExcepcion == avanzarPorExcepcion)
                .ToListAsync();

            Console.WriteLine($"🔀 Paso {paso.IdPasoSolicitud} ({paso.Nombre}) busca conexiones. Excepción: {avanzarPorExcepcion}. Encontradas: {conexiones.Count}");

            foreach (var conexion in conexiones)
            {
                var destino = await _context.PasosSolicitud.FirstOrDefaultAsync(p => p.IdPasoSolicitud == conexion.PasoDestinoId);
                if (destino == null) continue;

                // ✅ NUEVO: Si es camino de excepción, resetear pasos intermedios
                if (avanzarPorExcepcion && conexion.EsExcepcion)
                {
                    Console.WriteLine($"⚠️  Camino de excepción detectado: {paso.IdPasoSolicitud} -> {destino.IdPasoSolicitud}");
                    
                    try
                    {
                        await _resetService.ResetearPasosIntermediosAsync(
                            paso.IdPasoSolicitud,
                            destino.IdPasoSolicitud,
                            paso.FlujoActivoId
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error al resetear pasos intermedios: {ex.Message}");
                        // Continuar con la activación del destino aunque el reset falle
                    }
                }

                // Para conexiones de éxito (normales): esperar a que todos los orígenes normales estén completos con éxito (manejo de unión)
                if (!avanzarPorExcepcion)
                {
                    var origenesNormales = await _context.ConexionesPasoSolicitud
                        .Where(c => c.PasoDestinoId == destino.IdPasoSolicitud && !c.EsExcepcion)
                        .Select(c => c.PasoOrigenId)
                        .ToListAsync();

                    if (origenesNormales.Count > 1)
                    {
                        var pasosOrigen = await _context.PasosSolicitud
                            .Where(p => origenesNormales.Contains(p.IdPasoSolicitud))
                            .Select(p => new { p.IdPasoSolicitud, p.Estado })
                            .ToListAsync();

                        var todosListos = pasosOrigen.All(po => EsExito(po.Estado));
                        if (!todosListos)
                        {
                            // Aún no activar el destino hasta que lleguen todos los orígenes
                            Console.WriteLine($"⏸️  Esperando que todos los orígenes normales completen antes de activar paso {destino.IdPasoSolicitud}");
                            continue;
                        }
                    }
                }

                // Activar el paso destino
                destino.Estado = EstadoPasoSolicitud.Pendiente;
                destino.FechaInicio = DateTime.UtcNow;
                destino.FechaFin = null;
                
                _context.Entry(destino).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Paso {destino.IdPasoSolicitud} ({destino.Nombre}) activado como destino");
            }
        }

        private async Task CerrarFlujoSiCorresponde(PasoSolicitud pasoFin)
        {
            // Cerrar el FlujoActivo si existe y no está ya finalizado/cancelado
            var flujo = await _context.FlujosActivos.FirstOrDefaultAsync(f => f.IdFlujoActivo == pasoFin.FlujoActivoId);
            if (flujo == null) return;
            if (flujo.Estado == EstadoFlujoActivo.Finalizado || flujo.Estado == EstadoFlujoActivo.Cancelado) return;

            flujo.Estado = EstadoFlujoActivo.Finalizado;
            flujo.FechaFinalizacion = DateTime.UtcNow;
            _context.Entry(flujo).State = EntityState.Modified;
            await _context.SaveChangesAsync();
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
                            .ThenInclude(rug => rug.Usuario)
                .Include(p => p.RelacionesGrupoAprobacion)
                    .ThenInclude(r => r.Decisiones)
                    .ThenInclude(d => d.Usuario)
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
                SolicitudId = item.Relacion.SolicitudId,
                JsonOptions = item.Relacion.OptionsJson
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
                            .ThenInclude(rug => rug.Usuario)
                .Include(p => p.RelacionesGrupoAprobacion)
                    .ThenInclude(rga => rga.Decisiones)
                    .ThenInclude(d => d.Usuario)
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