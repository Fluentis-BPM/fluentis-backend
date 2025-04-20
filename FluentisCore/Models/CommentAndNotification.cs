using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentisCore.Models.UserManagement;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.Models.CommentAndNotificationManagement
{
    public enum PrioridadNotificacion { Baja, Media, Alta, Critica }

    public class Comentario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdComentario { get; set; }

        public int? PasoSolicitudId { get; set; }

        [ForeignKey("PasoSolicitudId")]
        public virtual PasoSolicitud PasoSolicitud { get; set; }

        public int? FlujoActivoId { get; set; }

        [ForeignKey("FlujoActivoId")]
        public virtual FlujoActivo FlujoActivo { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }

        public string Contenido { get; set; }

        public DateTime Fecha { get; set; }
    }

    public class Notificacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdNotificacion { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }

        public string Mensaje { get; set; }

        public PrioridadNotificacion Prioridad { get; set; }

        public bool Leida { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaEnvio { get; set; }
    }
}