using System.ComponentModel.DataAnnotations;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.DTO
{
    // Read DTOs to avoid cycles
    public class SolicitudDto
    {
        public int IdSolicitud { get; set; }
        public int? FlujoBaseId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
        public int SolicitanteId { get; set; }
        public UsuarioMiniDto? Solicitante { get; set; }
        public DateTime FechaCreacion { get; set; }
        public EstadoSolicitud Estado { get; set; }
        public List<RelacionInputDto> Inputs { get; set; } = new();
        public List<RelacionGrupoAprobacionDto> GruposAprobacion { get; set; } = new();
    }

    public class UsuarioMiniDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class RelacionGrupoAprobacionDto
    {
        public int IdRelacion { get; set; }
        public int GrupoAprobacionId { get; set; }
        public int? PasoSolicitudId { get; set; }
        public int? SolicitudId { get; set; }
        public List<RelacionDecisionUsuarioDto> Decisiones { get; set; } = new();
    }

    public class RelacionDecisionUsuarioDto
    {
        public int IdRelacion { get; set; }
        public int IdUsuario { get; set; }
        public bool? Decision { get; set; }
        public DateTime FechaDecision { get; set; }
        public UsuarioMiniDto? Usuario { get; set; }
    }

    // DTOs para la creación y actualización
    public class SolicitudCreateDto
    {
        [Required]
        public int SolicitanteId { get; set; }

        [Required]
        [StringLength(255)]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
        public int? FlujoBaseId { get; set; }
    public List<RelacionInputCreateDto> Inputs { get; set; } = new();
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
