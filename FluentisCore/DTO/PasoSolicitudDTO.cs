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
        public string Nombre { get; set; } = string.Empty;
        public ReglaAprobacion? ReglaAprobacion { get; set; } // Opcional para tipos Inicio y Fin
        public List<RelacionInputCreateDto>? Inputs { get; set; } // Incluye detalles al crear
        public int? PosX { get; set; }
        public int? PosY { get; set; }
    }

    public class PasoSolicitudUpdateDto
    {
        // Hacer opcional para permitir actualizaciones parciales sin forzar estado a Pendiente
        public EstadoPasoSolicitud? Estado { get; set; }
        public DateTime? FechaFin { get; set; }
        public int? ResponsableId { get; set; }
        public string? Nombre { get; set; }
        public int? PosX { get; set; }
        public int? PosY { get; set; }
    }





    public class ComentarioCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }
        [Required]
        public string Contenido { get; set; } = string.Empty;
    }

    public class ExcepcionCreateDto
    {
        [Required]
        public int UsuarioId { get; set; }
        [Required]
        public string Motivo { get; set; } = string.Empty;
    }

     public class ConexionCreateDto
    {
        public int DestinoId { get; set; }
        public bool EsExcepcion { get; set; } = false;
    }
}