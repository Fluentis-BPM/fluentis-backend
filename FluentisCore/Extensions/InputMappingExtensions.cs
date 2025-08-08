using FluentisCore.DTO;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.Extensions
{
    /// <summary>
    /// Métodos de extensión para convertir entre Modelos y DTOs
    /// Maneja la lógica de conversión compleja para valores de input tipados
    /// </summary>
    public static class InputMappingExtensions
    {
        /// <summary>
        /// Convierte el modelo RelacionInput a DTO con manejo de valores type-safe
        /// </summary>
        public static RelacionInputDto ToDto(this RelacionInput model)
        {
            return new RelacionInputDto
            {
                IdRelacion = model.IdRelacion,
                InputId = model.InputId,
                Nombre = model.Nombre,
                PlaceHolder = model.PlaceHolder,
                Requerido = model.Requerido,
                PasoSolicitudId = model.PasoSolicitudId,
                SolicitudId = model.SolicitudId,
                InputValue = model.Input != null ? new InputValueDto
                {
                    TipoInput = model.Input.TipoInput,
                    RawValue = model.Valor
                } : null
            };
        }

        /// <summary>
        /// Convierte RelacionInputCreateDto a modelo RelacionInput
        /// </summary>
        public static RelacionInput ToModel(this RelacionInputCreateDto dto)
        {
            return new RelacionInput
            {
                InputId = dto.InputId,
                Nombre = dto.Nombre,
                PlaceHolder = dto.PlaceHolder,
                Requerido = dto.Requerido,
                Valor = dto.Valor?.RawValue
            };
        }

        /// <summary>
        /// Actualiza un modelo RelacionInput existente desde DTO
        /// </summary>
        public static void UpdateFromDto(this RelacionInput model, RelacionInputUpdateDto dto)
        {
            if (dto.InputId.HasValue)
                model.InputId = dto.InputId.Value;
            
            if (!string.IsNullOrEmpty(dto.Nombre))
                model.Nombre = dto.Nombre;
            
            if (dto.PlaceHolder != null)
                model.PlaceHolder = dto.PlaceHolder;
            
            if (dto.Requerido.HasValue)
                model.Requerido = dto.Requerido.Value;
            
            if (dto.Valor != null)
                model.Valor = dto.Valor.RawValue;
        }

        /// <summary>
        /// Valida que el valor del input coincida con el tipo esperado
        /// </summary>
        public static bool IsValidForType(this InputValueDto inputValue)
        {
            if (string.IsNullOrEmpty(inputValue.RawValue))
                return true; // Los valores nulos son manejados por la validación Required

            try
            {
                return inputValue.TipoInput switch
                {
                    TipoInput.TextoCorto => true, // Ya validamos que no es null/empty arriba
                    TipoInput.TextoLargo => true, // Ya validamos que no es null/empty arriba
                    TipoInput.Date => DateTime.TryParse(inputValue.RawValue, out _),
                    TipoInput.Number => decimal.TryParse(inputValue.RawValue, out _),
                    TipoInput.Combobox => true, // Ya validamos que no es null/empty arriba
                    TipoInput.MultipleCheckbox => IsValidJsonArray(inputValue.RawValue),
                    TipoInput.Archivo => IsValidFileInfo(inputValue.RawValue),
                    _ => true
                };
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidJsonArray(string json)
        {
            try
            {
                System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidFileInfo(string json)
        {
            try
            {
                System.Text.Json.JsonSerializer.Deserialize<FileInfoDto>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
