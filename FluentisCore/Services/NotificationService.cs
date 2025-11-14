using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentisCore.Models;
using FluentisCore.Models.CommentAndNotificationManagement;
using FluentisCore.Models.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FluentisCore.Services
{
    /// <summary>
    /// Servicio centralizado para la gestión de notificaciones a usuarios.
    /// Permite enviar notificaciones individuales, masivas y con diferentes prioridades.
    /// </summary>
    public class NotificationService
    {
        private readonly FluentisContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(FluentisContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea una notificación para un solo usuario.
        /// </summary>
        /// <param name="usuarioId">ID del usuario destinatario</param>
        /// <param name="mensaje">Contenido del mensaje</param>
        /// <param name="prioridad">Nivel de prioridad de la notificación</param>
        /// <returns>La notificación creada</returns>
        public async Task<Notificacion> CrearNotificacionAsync(
            int usuarioId, 
            string mensaje, 
            PrioridadNotificacion prioridad = PrioridadNotificacion.Media)
        {
            try
            {
                // Validar que el usuario existe
                var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.IdUsuario == usuarioId);
                if (!usuarioExiste)
                {
                    _logger.LogWarning($"Intento de crear notificación para usuario inexistente: {usuarioId}");
                    throw new ArgumentException($"El usuario con ID {usuarioId} no existe.");
                }

                var notificacion = new Notificacion
                {
                    UsuarioId = usuarioId,
                    Mensaje = mensaje,
                    Prioridad = prioridad,
                    Leida = false,
                    FechaEnvio = DateTime.UtcNow
                };

                _context.Notificaciones.Add(notificacion);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Notificación creada para usuario {usuarioId}: {mensaje}");

                return notificacion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear notificación para usuario {usuarioId}");
                throw;
            }
        }

        /// <summary>
        /// Crea notificaciones para múltiples usuarios con el mismo mensaje.
        /// </summary>
        /// <param name="usuariosIds">Lista de IDs de usuarios destinatarios</param>
        /// <param name="mensaje">Contenido del mensaje</param>
        /// <param name="prioridad">Nivel de prioridad de la notificación</param>
        /// <returns>Lista de notificaciones creadas</returns>
        public async Task<List<Notificacion>> CrearNotificacionesMasivasAsync(
            List<int> usuariosIds, 
            string mensaje, 
            PrioridadNotificacion prioridad = PrioridadNotificacion.Media)
        {
            try
            {
                if (usuariosIds == null || !usuariosIds.Any())
                {
                    _logger.LogWarning("Intento de crear notificaciones masivas sin usuarios");
                    return new List<Notificacion>();
                }

                // Validar que todos los usuarios existen
                var usuariosExistentes = await _context.Usuarios
                    .Where(u => usuariosIds.Contains(u.IdUsuario))
                    .Select(u => u.IdUsuario)
                    .ToListAsync();

                var usuariosInvalidos = usuariosIds.Except(usuariosExistentes).ToList();
                if (usuariosInvalidos.Any())
                {
                    _logger.LogWarning($"Usuarios no encontrados: {string.Join(", ", usuariosInvalidos)}");
                }

                var notificaciones = new List<Notificacion>();
                var fechaEnvio = DateTime.UtcNow;

                foreach (var usuarioId in usuariosExistentes)
                {
                    var notificacion = new Notificacion
                    {
                        UsuarioId = usuarioId,
                        Mensaje = mensaje,
                        Prioridad = prioridad,
                        Leida = false,
                        FechaEnvio = fechaEnvio
                    };

                    notificaciones.Add(notificacion);
                }

                _context.Notificaciones.AddRange(notificaciones);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Notificaciones masivas creadas para {notificaciones.Count} usuarios: {mensaje}");

                return notificaciones;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear notificaciones masivas");
                throw;
            }
        }

        /// <summary>
        /// Crea notificaciones personalizadas para múltiples usuarios (cada uno con su propio mensaje).
        /// </summary>
        /// <param name="notificacionesData">Diccionario con usuarioId como clave y tupla (mensaje, prioridad) como valor</param>
        /// <returns>Lista de notificaciones creadas</returns>
        public async Task<List<Notificacion>> CrearNotificacionesPersonalizadasAsync(
            Dictionary<int, (string mensaje, PrioridadNotificacion prioridad)> notificacionesData)
        {
            try
            {
                if (notificacionesData == null || !notificacionesData.Any())
                {
                    _logger.LogWarning("Intento de crear notificaciones personalizadas sin datos");
                    return new List<Notificacion>();
                }

                var usuariosIds = notificacionesData.Keys.ToList();
                var usuariosExistentes = await _context.Usuarios
                    .Where(u => usuariosIds.Contains(u.IdUsuario))
                    .Select(u => u.IdUsuario)
                    .ToListAsync();

                var notificaciones = new List<Notificacion>();
                var fechaEnvio = DateTime.UtcNow;

                foreach (var usuarioId in usuariosExistentes)
                {
                    if (notificacionesData.TryGetValue(usuarioId, out var data))
                    {
                        var notificacion = new Notificacion
                        {
                            UsuarioId = usuarioId,
                            Mensaje = data.mensaje,
                            Prioridad = data.prioridad,
                            Leida = false,
                            FechaEnvio = fechaEnvio
                        };

                        notificaciones.Add(notificacion);
                    }
                }

                _context.Notificaciones.AddRange(notificaciones);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Notificaciones personalizadas creadas para {notificaciones.Count} usuarios");

                return notificaciones;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear notificaciones personalizadas");
                throw;
            }
        }

        /// <summary>
        /// Notifica la asignación de un paso de ejecución a un usuario.
        /// </summary>
        public async Task<Notificacion> NotificarAsignacionPasoAsync(
            int usuarioId, 
            int pasoId, 
            string nombrePaso, 
            string nombreSolicitud = "")
        {
            var mensaje = string.IsNullOrEmpty(nombreSolicitud)
                ? $"Se te ha asignado un nuevo paso: {nombrePaso}"
                : $"Se te ha asignado un nuevo paso '{nombrePaso}' en la solicitud '{nombreSolicitud}'";

            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Alta);
        }

        /// <summary>
        /// Notifica a múltiples usuarios que requieren aprobar un paso.
        /// </summary>
        public async Task<List<Notificacion>> NotificarAprobacionRequeridaAsync(
            List<int> usuariosIds, 
            int pasoId, 
            string nombrePaso, 
            string nombreSolicitud = "")
        {
            var mensaje = string.IsNullOrEmpty(nombreSolicitud)
                ? $"Se requiere tu aprobación para: {nombrePaso}"
                : $"Se requiere tu aprobación para el paso '{nombrePaso}' en la solicitud '{nombreSolicitud}'";

            return await CrearNotificacionesMasivasAsync(usuariosIds, mensaje, PrioridadNotificacion.Alta);
        }

        /// <summary>
        /// Notifica cambio de estado de un paso.
        /// </summary>
        public async Task<Notificacion> NotificarCambioEstadoPasoAsync(
            int usuarioId, 
            string nombrePaso, 
            string estadoAnterior, 
            string estadoNuevo)
        {
            var mensaje = $"El paso '{nombrePaso}' cambió de estado: {estadoAnterior} → {estadoNuevo}";
            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Media);
        }

        /// <summary>
        /// Notifica finalización de un flujo a usuarios relacionados.
        /// </summary>
        public async Task<List<Notificacion>> NotificarFinalizacionFlujoAsync(
            List<int> usuariosIds, 
            string nombreFlujo, 
            string resultado = "completado")
        {
            var mensaje = $"El flujo '{nombreFlujo}' ha sido {resultado}";
            return await CrearNotificacionesMasivasAsync(usuariosIds, mensaje, PrioridadNotificacion.Media);
        }

        /// <summary>
        /// Notifica que se agregó un comentario en un paso.
        /// </summary>
        public async Task<Notificacion> NotificarComentarioAgregadoAsync(
            int usuarioId, 
            string nombrePaso, 
            string autorComentario)
        {
            var mensaje = $"{autorComentario} agregó un comentario en '{nombrePaso}'";
            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Baja);
        }

        /// <summary>
        /// Notifica una excepción registrada en un paso.
        /// </summary>
        public async Task<List<Notificacion>> NotificarExcepcionPasoAsync(
            List<int> usuariosIds, 
            string nombrePaso, 
            string motivo)
        {
            var mensaje = $"Excepción registrada en '{nombrePaso}': {motivo}";
            return await CrearNotificacionesMasivasAsync(usuariosIds, mensaje, PrioridadNotificacion.Critica);
        }

        /// <summary>
        /// Notifica a un grupo de aprobación completo.
        /// </summary>
        public async Task<List<Notificacion>> NotificarGrupoAprobacionAsync(
            int grupoAprobacionId, 
            string mensaje, 
            PrioridadNotificacion prioridad = PrioridadNotificacion.Alta)
        {
            try
            {
                var usuariosIds = await _context.RelacionesUsuarioGrupo
                    .Where(r => r.GrupoAprobacionId == grupoAprobacionId)
                    .Select(r => r.UsuarioId)
                    .Distinct()
                    .ToListAsync();

                if (!usuariosIds.Any())
                {
                    _logger.LogWarning($"Grupo de aprobación {grupoAprobacionId} no tiene usuarios asociados");
                    return new List<Notificacion>();
                }

                return await CrearNotificacionesMasivasAsync(usuariosIds, mensaje, prioridad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al notificar grupo de aprobación {grupoAprobacionId}");
                throw;
            }
        }

        /// <summary>
        /// Notifica a todos los usuarios de un departamento.
        /// </summary>
        public async Task<List<Notificacion>> NotificarDepartamentoAsync(
            int departamentoId, 
            string mensaje, 
            PrioridadNotificacion prioridad = PrioridadNotificacion.Media)
        {
            try
            {
                var usuariosIds = await _context.Usuarios
                    .Where(u => u.DepartamentoId == departamentoId)
                    .Select(u => u.IdUsuario)
                    .ToListAsync();

                if (!usuariosIds.Any())
                {
                    _logger.LogWarning($"Departamento {departamentoId} no tiene usuarios asociados");
                    return new List<Notificacion>();
                }

                return await CrearNotificacionesMasivasAsync(usuariosIds, mensaje, prioridad);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al notificar departamento {departamentoId}");
                throw;
            }
        }

        /// <summary>
        /// Marca una notificación como leída.
        /// </summary>
        public async Task<bool> MarcarComoLeidaAsync(int notificacionId)
        {
            try
            {
                var notificacion = await _context.Notificaciones.FindAsync(notificacionId);
                if (notificacion == null)
                {
                    _logger.LogWarning($"Notificación {notificacionId} no encontrada");
                    return false;
                }

                notificacion.Leida = true;
                _context.Entry(notificacion).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al marcar notificación {notificacionId} como leída");
                throw;
            }
        }

        /// <summary>
        /// Marca múltiples notificaciones como leídas.
        /// </summary>
        public async Task<int> MarcarVariasComoLeidasAsync(List<int> notificacionesIds)
        {
            try
            {
                var notificaciones = await _context.Notificaciones
                    .Where(n => notificacionesIds.Contains(n.IdNotificacion) && !n.Leida)
                    .ToListAsync();

                foreach (var notificacion in notificaciones)
                {
                    notificacion.Leida = true;
                    _context.Entry(notificacion).State = EntityState.Modified;
                }

                await _context.SaveChangesAsync();
                return notificaciones.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al marcar múltiples notificaciones como leídas");
                throw;
            }
        }

        /// <summary>
        /// Obtiene el conteo de notificaciones no leídas de un usuario.
        /// </summary>
        public async Task<int> ObtenerNotificacionesNoLeidasCountAsync(int usuarioId)
        {
            return await _context.Notificaciones
                .Where(n => n.UsuarioId == usuarioId && !n.Leida)
                .CountAsync();
        }

        /// <summary>
        /// Elimina notificaciones antiguas (limpieza de mantenimiento).
        /// </summary>
        public async Task<int> EliminarNotificacionesAntiguasAsync(int diasAntiguedad = 90)
        {
            try
            {
                var fechaLimite = DateTime.UtcNow.AddDays(-diasAntiguedad);
                var notificacionesAntiguas = await _context.Notificaciones
                    .Where(n => n.FechaEnvio < fechaLimite && n.Leida)
                    .ToListAsync();

                _context.Notificaciones.RemoveRange(notificacionesAntiguas);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Eliminadas {notificacionesAntiguas.Count} notificaciones antiguas");
                return notificacionesAntiguas.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar notificaciones antiguas");
                throw;
            }
        }

        /// <summary>
        /// Notifica mención en comentario (@usuario).
        /// </summary>
        public async Task<List<Notificacion>> NotificarMencionEnComentarioAsync(
            List<int> usuariosIds, 
            string nombrePaso, 
            string autorMencion)
        {
            var mensaje = $"{autorMencion} te mencionó en un comentario de '{nombrePaso}'";
            return await CrearNotificacionesMasivasAsync(usuariosIds, mensaje, PrioridadNotificacion.Media);
        }

        /// <summary>
        /// Notifica cambio de estado de solicitud.
        /// </summary>
        public async Task<Notificacion> NotificarCambioEstadoSolicitudAsync(
            int usuarioId,
            string tituloSolicitud,
            string estadoAnterior,
            string estadoNuevo)
        {
            var mensaje = $"La solicitud '{tituloSolicitud}' cambió de {estadoAnterior} a {estadoNuevo}";
            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Alta);
        }

        /// <summary>
        /// Notifica inicio de flujo al responsable del primer paso.
        /// </summary>
        public async Task<Notificacion> NotificarInicioFlujoAsync(
            int usuarioId,
            string nombreFlujo,
            string tituloSolicitud = "")
        {
            var mensaje = string.IsNullOrEmpty(tituloSolicitud)
                ? $"Se inició el flujo '{nombreFlujo}'"
                : $"Se inició el flujo '{nombreFlujo}' para la solicitud '{tituloSolicitud}'";
            
            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Alta);
        }

        /// <summary>
        /// Notifica asignación de rol.
        /// </summary>
        public async Task<Notificacion> NotificarAsignacionRolAsync(
            int usuarioId,
            string nombreRol)
        {
            var mensaje = $"Se te ha asignado el rol: {nombreRol}";
            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Media);
        }

        /// <summary>
        /// Notifica agregación a grupo de aprobación.
        /// </summary>
        public async Task<Notificacion> NotificarAgregarGrupoAsync(
            int usuarioId,
            string nombreGrupo)
        {
            var mensaje = $"Has sido agregado al grupo de aprobación: {nombreGrupo}";
            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Media);
        }

        /// <summary>
        /// Notifica remoción de grupo de aprobación.
        /// </summary>
        public async Task<Notificacion> NotificarRemoverGrupoAsync(
            int usuarioId,
            string nombreGrupo)
        {
            var mensaje = $"Has sido removido del grupo de aprobación: {nombreGrupo}";
            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Baja);
        }

        /// <summary>
        /// Notifica decisión de aprobación a creador de solicitud.
        /// </summary>
        public async Task<Notificacion> NotificarDecisionAprobacionAsync(
            int usuarioId,
            string nombrePaso,
            string nombreAprobador,
            bool aprobado,
            string comentario = "")
        {
            var accion = aprobado ? "aprobó" : "rechazó";
            var mensaje = string.IsNullOrEmpty(comentario)
                ? $"{nombreAprobador} {accion} el paso '{nombrePaso}'"
                : $"{nombreAprobador} {accion} el paso '{nombrePaso}': {comentario}";

            var prioridad = aprobado ? PrioridadNotificacion.Media : PrioridadNotificacion.Alta;
            return await CrearNotificacionAsync(usuarioId, mensaje, prioridad);
        }

        /// <summary>
        /// Notifica creación de solicitud.
        /// </summary>
        public async Task<Notificacion> NotificarCreacionSolicitudAsync(
            int usuarioId,
            string tituloSolicitud)
        {
            var mensaje = $"Tu solicitud '{tituloSolicitud}' ha sido creada exitosamente";
            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Media);
        }

        /// <summary>
        /// Notifica cancelación de solicitud a todos los involucrados.
        /// </summary>
        public async Task<List<Notificacion>> NotificarCancelacionSolicitudAsync(
            List<int> usuariosIds,
            string tituloSolicitud,
            string motivoCancelacion = "")
        {
            var mensaje = string.IsNullOrEmpty(motivoCancelacion)
                ? $"La solicitud '{tituloSolicitud}' ha sido cancelada"
                : $"La solicitud '{tituloSolicitud}' ha sido cancelada: {motivoCancelacion}";

            return await CrearNotificacionesMasivasAsync(usuariosIds, mensaje, PrioridadNotificacion.Alta);
        }

        /// <summary>
        /// Notifica cambio de responsable de paso.
        /// </summary>
        public async Task<Dictionary<int, Notificacion>> NotificarCambioResponsableAsync(
            int responsableAnteriorId,
            int nuevoResponsableId,
            string nombrePaso)
        {
            var notificaciones = new Dictionary<int, Notificacion>();

            // Notificar al responsable anterior
            var notifAnterior = await CrearNotificacionAsync(
                responsableAnteriorId,
                $"Ya no eres responsable del paso '{nombrePaso}'",
                PrioridadNotificacion.Baja
            );
            notificaciones[responsableAnteriorId] = notifAnterior;

            // Notificar al nuevo responsable
            var notifNuevo = await CrearNotificacionAsync(
                nuevoResponsableId,
                $"Se te ha asignado como responsable del paso '{nombrePaso}'",
                PrioridadNotificacion.Alta
            );
            notificaciones[nuevoResponsableId] = notifNuevo;

            return notificaciones;
        }

        /// <summary>
        /// Notifica vencimiento próximo de un paso.
        /// </summary>
        public async Task<Notificacion> NotificarVencimientoProximoAsync(
            int usuarioId,
            string nombrePaso,
            DateTime fechaVencimiento)
        {
            var tiempoRestante = fechaVencimiento - DateTime.UtcNow;
            var mensaje = tiempoRestante.TotalHours < 24
                ? $"⚠️ El paso '{nombrePaso}' vence en {tiempoRestante.Hours} horas"
                : $"⚠️ El paso '{nombrePaso}' vence el {fechaVencimiento:dd/MM/yyyy HH:mm}";

            return await CrearNotificacionAsync(usuarioId, mensaje, PrioridadNotificacion.Critica);
        }

        /// <summary>
        /// Notifica que un paso ha vencido.
        /// </summary>
        public async Task<List<Notificacion>> NotificarPasoVencidoAsync(
            List<int> usuariosIds,
            string nombrePaso)
        {
            var mensaje = $"🔴 El paso '{nombrePaso}' ha vencido sin completarse";
            return await CrearNotificacionesMasivasAsync(usuariosIds, mensaje, PrioridadNotificacion.Critica);
        }
    }
}
