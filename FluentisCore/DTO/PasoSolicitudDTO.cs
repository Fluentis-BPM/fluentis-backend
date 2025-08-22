using System.ComponentModel.DataAnnotations;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.DTO
{
    public class PasoSolicitudCreateDto
    {
        [Required]
        public int FlujoActivoId { get; set; }
        public int? PasoOrigenId { get; set; } // Para vincular al paso anterior
        public int? ResponsableId { get; set; }
        [Required]
        public TipoPaso TipoPaso { get; set; }
        public EstadoPasoSolicitud Estado { get; set; } = EstadoPasoSolicitud.Pendiente; // Estado por defecto
        public string Nombre { get; set; }
        [Required]
        public ReglaAprobacion ReglaAprobacion { get; set; } // Cambiado de ReglaAprobacion
        public List<RelacionInputCreateDto> Inputs { get; set; } // Incluye detalles al crear
    }

    public class PasoSolicitudUpdateDto
    {
        public EstadoPasoSolicitud Estado { get; set; } = EstadoPasoSolicitud.Pendiente; // Estado por defecto
        public DateTime? FechaFin { get; set; }
        public int? ResponsableId { get; set; }
        public string Nombre { get; set; }
    }





    public class ComentarioCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }
        [Required]
        public string Contenido { get; set; }
    }

    public class ExcepcionCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }
        [Required]
        public string Motivo { get; set; }
    }

     public class ConexionCreateDto
    {
        public int DestinoId { get; set; }
        public bool EsExcepcion { get; set; } = false;
    }
}