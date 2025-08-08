using System.ComponentModel.DataAnnotations;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.DTO
{
    public class InputCreateDto
    {
        public bool? EsJson { get; set; }
        [Required]
        public TipoInput TipoInput { get; set; }
    }

    public class InputUpdateDto
    {
        public bool? EsJson { get; set; }
        [Required]
        public TipoInput TipoInput { get; set; }
    }

    /// <summary>
    /// DTO de Input mejorado que incluye metadatos para el renderizado de la interfaz
    /// </summary>
    public class InputDto
    {
        public int IdInput { get; set; }
        public bool? EsJson { get; set; }
        public TipoInput TipoInput { get; set; }
        
        /// <summary>
        /// Metadatos para que los componentes de la interfaz puedan renderizar correctamente este tipo de input
        /// </summary>
        public InputMetadataDto? Metadata { get; set; }
    }

    /// <summary>
    /// Metadatos para diferentes tipos de input para ayudar al renderizado del frontend
    /// </summary>
    public class InputMetadataDto
    {
        /// <summary>
        /// Para Combobox y MultipleCheckbox - opciones predefinidas
        /// </summary>
        public List<string>? Options { get; set; }
        
        /// <summary>
        /// Reglas de validación por defecto para este tipo de input
        /// </summary>
        public InputValidationDto? DefaultValidation { get; set; }
        
        /// <summary>
        /// Sugerencias de interfaz para el renderizado
        /// </summary>
        public Dictionary<string, object>? UiHints { get; set; }
    }
}