using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.Models.TemplateManagement
{
    /// <summary>
    /// Plantilla reusable para crear nuevas Solicitudes preconfiguradas
    /// </summary>
    public class PlantillaSolicitud
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPlantilla { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        /// <summary>
        /// Flujo base a asociar a la solicitud creada desde esta plantilla (opcional)
        /// </summary>
        public int? FlujoBaseId { get; set; }

        [ForeignKey("FlujoBaseId")]
        public virtual FlujoAprobacion? FlujoBase { get; set; }

        /// <summary>
        /// Grupo de aprobación por defecto a asociar a la solicitud (opcional)
        /// </summary>
        public int? GrupoAprobacionId { get; set; }

        [ForeignKey("GrupoAprobacionId")]
        public virtual GrupoAprobacion? GrupoAprobacion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public virtual ICollection<PlantillaInput> Inputs { get; set; } = new List<PlantillaInput>();
    }

    /// <summary>
    /// Definición de un input dentro de una Plantilla de Solicitud
    /// </summary>
    public class PlantillaInput
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPlantillaInput { get; set; }

        [Required]
        public int PlantillaSolicitudId { get; set; }

        [ForeignKey("PlantillaSolicitudId")]
        public virtual PlantillaSolicitud Plantilla { get; set; } = null!;

        [Required]
        public int InputId { get; set; }

        [ForeignKey("InputId")]
        public virtual Inputs Input { get; set; } = null!;

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(255)]
        public string? PlaceHolder { get; set; }

        public bool Requerido { get; set; }

        /// <summary>
        /// Valor por defecto en formato string (se interpreta según el TipoInput)
        /// </summary>
        public string? ValorPorDefecto { get; set; }

        /// <summary>
        /// Opciones (JSON array) para tipos basados en lista: Combobox, MultipleCheckbox, RadioGroup
        /// Se almacenan como JSON para simplicidad y flexibilidad.
        /// </summary>
        public string? OpcionesJson { get; set; }
    }
}
