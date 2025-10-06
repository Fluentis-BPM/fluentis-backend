using System.ComponentModel.DataAnnotations;
using FluentisCore.Models.TemplateManagement;

namespace FluentisCore.DTO
{
    public class PlantillaInputDto
    {
        public int IdPlantillaInput { get; set; }
        public int InputId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? PlaceHolder { get; set; }
        public bool Requerido { get; set; }
        public string? ValorPorDefecto { get; set; }
        public List<string>? Opciones { get; set; }
    }

    public class PlantillaSolicitudDto
    {
        public int IdPlantilla { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int? FlujoBaseId { get; set; }
        public int? GrupoAprobacionId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public List<PlantillaInputDto> Inputs { get; set; } = new();
    }

    public class PlantillaInputCreateDto
    {
        [Required]
        public int InputId { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255)]
        public string? PlaceHolder { get; set; }

        public bool Requerido { get; set; }

        public string? ValorPorDefecto { get; set; }
        public List<string>? Opciones { get; set; }
    }

    public class PlantillaSolicitudCreateDto
    {
        [Required]
        [StringLength(255)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }
        public int? FlujoBaseId { get; set; }
        public int? GrupoAprobacionId { get; set; }
        public List<PlantillaInputCreateDto> Inputs { get; set; } = new();
    }

    public class PlantillaSolicitudUpdateDto
    {
        [Required]
        public int IdPlantilla { get; set; }

        [StringLength(255)]
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public int? FlujoBaseId { get; set; }
        public int? GrupoAprobacionId { get; set; }
        public List<PlantillaInputCreateDto>? Inputs { get; set; }
    }

    public class InstanciarSolicitudDesdePlantillaDto
    {
        [Required]
        public int PlantillaId { get; set; }

    // Opcional: si no se envía, se derivará del usuario autenticado
    public int SolicitanteId { get; set; }

        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public Dictionary<int, string>? OverridesValores { get; set; } // key: InputId, value: raw string value
    }
}
