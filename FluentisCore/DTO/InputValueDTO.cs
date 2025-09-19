using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.DTO
{
    /// <summary>
    /// DTO for handling dynamic input values based on TipoInput
    /// Provides type-safe serialization/deserialization for different input types
    /// </summary>
    public class InputValueDto
    {
        [Required]
        public TipoInput TipoInput { get; set; }

        /// <summary>
        /// The raw value as stored in the database (string format)
        /// </summary>
        public string? RawValue { get; set; }

        /// <summary>
        /// Typed value property that handles conversion based on TipoInput
        /// This will serialize/deserialize according to the input type
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value 
        { 
            get => ConvertFromRawValue();
            set => RawValue = ConvertToRawValue(value);
        }

        /// <summary>
        /// For ComboBox and MultipleCheckbox types - available options
        /// </summary>
        public List<string>? Options { get; set; }

        /// <summary>
        /// Validation constraints based on input type
        /// </summary>
        public InputValidationDto? Validation { get; set; }

        private object? ConvertFromRawValue()
        {
            if (string.IsNullOrEmpty(RawValue))
                return null;

            try
            {
                return TipoInput switch
                {
                    TipoInput.TextoCorto => RawValue,
                    TipoInput.TextoLargo => RawValue,
                    TipoInput.Date => DateTime.TryParse(RawValue, out var date) ? date : RawValue,
                    TipoInput.Number => decimal.TryParse(RawValue, out var number) ? number : RawValue,
                    TipoInput.Combobox => RawValue,
                    // Tolerar valores no JSON devolviendo el string crudo para evitar 500 en serialización
            TipoInput.MultipleCheckbox => string.IsNullOrEmpty(RawValue) ? null : (object?)TryDeserializeOrFallback<List<string>>(RawValue, RawValue) ?? RawValue,
            TipoInput.Archivo => string.IsNullOrEmpty(RawValue) ? null : (object?)TryDeserializeOrFallback<FileInfoDto>(RawValue, RawValue) ?? RawValue,
                    _ => RawValue
                };
            }
            catch
            {
                // Si algo falla durante la conversión, devolver el valor crudo para no romper la respuesta
                return RawValue;
            }
        }

    private static TOut? TryDeserializeOrFallback<TOut>(string json, object? fallback)
        {
            try
            {
                return JsonSerializer.Deserialize<TOut>(json);
            }
            catch
            {
        return fallback is TOut t ? t : default;
            }
        }

        private string? ConvertToRawValue(object? value)
        {
            if (value == null)
                return null;

            return TipoInput switch
            {
                TipoInput.TextoCorto => value.ToString(),
                TipoInput.TextoLargo => value.ToString(),
                TipoInput.Date => value is DateTime dt ? dt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : value.ToString(),
                TipoInput.Number => value.ToString(),
                TipoInput.Combobox => value.ToString(),
                TipoInput.MultipleCheckbox => JsonSerializer.Serialize(value),
                TipoInput.Archivo => JsonSerializer.Serialize(value),
                _ => value.ToString()
            };
        }
    }

    /// <summary>
    /// Validation constraints for input values
    /// </summary>
    public class InputValidationDto
    {
        public bool Required { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
        public List<string>? AllowedExtensions { get; set; } // For file types
        public long? MaxFileSize { get; set; } // In bytes
    }

    /// <summary>
    /// DTO for file information when TipoInput is Archivo
    /// </summary>
    public class FileInfoDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string? Base64Content { get; set; } // Optional: for small files
        public string? FilePath { get; set; } // Optional: server file path
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Enhanced RelacionInput DTO with type-safe value handling
    /// </summary>
    public class RelacionInputDto
    {
        public int? IdRelacion { get; set; }
        
        [Required]
        public int InputId { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Nombre { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string? PlaceHolder { get; set; }
        
        public bool Requerido { get; set; }
        
        /// <summary>
        /// Type-safe input value with automatic conversion
        /// </summary>
        public InputValueDto? InputValue { get; set; }
        
        public int? PasoSolicitudId { get; set; }
        public int? SolicitudId { get; set; }
    }

    /// <summary>
    /// DTO for creating RelacionInput with typed values
    /// </summary>
    public class RelacionInputCreateDto
    {
        [Required]
        public int InputId { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Nombre { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string? PlaceHolder { get; set; }
        
        public bool Requerido { get; set; }
        
        /// <summary>
        /// Type-safe input value
        /// </summary>
        public InputValueDto? Valor { get; set; }
    }

    /// <summary>
    /// DTO for updating RelacionInput values
    /// </summary>
    public class RelacionInputUpdateDto
    {
        public int? InputId { get; set; }
        public int? IdRelacion { get; set; }
        
        [StringLength(255)]
        public string? Nombre { get; set; }
        
        [StringLength(255)]
        public string? PlaceHolder { get; set; }
        
        public bool? Requerido { get; set; }
        
        /// <summary>
        /// Type-safe input value for updates
        /// </summary>
        public InputValueDto? Valor { get; set; }
    }
}
