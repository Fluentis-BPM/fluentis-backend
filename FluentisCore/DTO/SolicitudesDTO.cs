using System.ComponentModel.DataAnnotations;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.DTO
{

    // DTOs para la creación y actualización
    public class SolicitudCreateDto
    {
        [Required]
        public int SolicitanteId { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }
        public int? FlujoBaseId { get; set; }
        public List<RelacionInputCreateDto> Inputs { get; set; }
        public int? GrupoAprobacionId { get; set; }
    }

    public class SolicitudUpdateDto
    {
        public int IdSolicitud { get; set; }
        [Required]
        [StringLength(50)]
        public EstadoSolicitud Estado { get; set; }
    }

    public class RelacionGrupoAprobacionCreateDto
    {
        [Required]
        public int GrupoAprobacionId { get; set; }
    }

    public class RelacionDecisionUsuarioCreateDto
    {
        [Required]
        public int IdUsuario { get; set; }
        [Required]
        public bool Decision { get; set; }
    }
}
