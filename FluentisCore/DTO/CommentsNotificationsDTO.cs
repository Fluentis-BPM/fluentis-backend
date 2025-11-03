using System;
using System.ComponentModel.DataAnnotations;
using FluentisCore.Models.CommentAndNotificationManagement;

namespace FluentisCore.DTO
{
    // ===== Comentarios =====

    public class ComentarioDto
    {
        public int IdComentario { get; set; }
        public int? PasoSolicitudId { get; set; }
        public int? FlujoActivoId { get; set; }
        public int UsuarioId { get; set; }
        public string Contenido { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }

        // Información breve del usuario (opcional)
        public UsuarioMiniDto? Usuario { get; set; }
    }

    // Usada para crear comentarios desde un endpoint general (fuera de PasoSolicitudController)
    public class ComentarioCreateGeneralDto
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public string Contenido { get; set; } = string.Empty;

        // Uno de los dos debe venir informado
        public int? PasoSolicitudId { get; set; }
        public int? FlujoActivoId { get; set; }
    }

    public class ComentarioUpdateDto
    {
        [Required]
        public string Contenido { get; set; } = string.Empty;
    }

    // ===== Notificaciones =====

    public class NotificacionDto
    {
        public int IdNotificacion { get; set; }
        public int UsuarioId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public PrioridadNotificacion Prioridad { get; set; }
        public bool Leida { get; set; }
        public DateTime FechaEnvio { get; set; }

        // Información breve del usuario (opcional)
        public UsuarioMiniDto? Usuario { get; set; }
    }

    public class NotificacionCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public string Mensaje { get; set; } = string.Empty;

        [Required]
        public PrioridadNotificacion Prioridad { get; set; } = PrioridadNotificacion.Media;
    }

    public class NotificacionUpdateDto
    {
        public string? Mensaje { get; set; }
        public PrioridadNotificacion? Prioridad { get; set; }
        public bool? Leida { get; set; }
    }
}
