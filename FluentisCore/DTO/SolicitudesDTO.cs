using System.ComponentModel.DataAnnotations;

namespace FluentisCore.DTO
{
    
    // DTOs para la creación y actualización
    public class SolicitudCreateDto
    {
        [Required]
        public int SolicitanteId { get; set; }
        public int? FlujoBaseId { get; set; }
        public List<RelacionInputCreateDto> Inputs { get; set; }
        public int? GrupoAprobacionId { get; set; }
    }

    public class SolicitudUpdateDto
    {
        public int IdSolicitud { get; set; }
        [Required]
        [StringLength(50)]
        public string Estado { get; set; }
    }

    public class RelacionInputCreateDto
    {
        [Required]
        public int InputId { get; set; }
        public string Valor { get; set; }
        public bool? Requerido { get; set; }
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
