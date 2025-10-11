using System.Text.Json;
using System.Text.Json.Serialization;
using FluentisCore.DTO;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.Converters
{
    /// <summary>
    /// Serializador para InputValueDto que maneja la conversión de valores dinámicos
    /// </summary>
    public class InputValueJsonConverter : JsonConverter<InputValueDto>
    {
        public override InputValueDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected StartObject token");

            var inputValue = new InputValueDto();
            
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propertyName = reader.GetString()?.ToLowerInvariant();
                reader.Read();

                switch (propertyName)
                {
                    case "tipoinput":
                        if (Enum.TryParse<TipoInput>(reader.GetString(), true, out var tipoInput))
                            inputValue.TipoInput = tipoInput;
                        break;
                    
                    case "value":
                        inputValue.RawValue = ReadValueBasedOnType(ref reader, inputValue.TipoInput);
                        break;
                    
                    case "options":
                        inputValue.Options = JsonSerializer.Deserialize<List<string>>(ref reader, options);
                        break;
                    
                    case "validation":
                        inputValue.Validation = JsonSerializer.Deserialize<InputValidationDto>(ref reader, options);
                        break;
                }
            }

            return inputValue;
        }

        public override void Write(Utf8JsonWriter writer, InputValueDto value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            
            // Write TipoInput
            writer.WriteString("tipoInput", value.TipoInput.ToString());
            
            // Write typed value
            WriteValueBasedOnType(writer, "value", value.Value, value.TipoInput, options);
            
            // Write options if present
            if (value.Options != null)
            {
                writer.WritePropertyName("options");
                JsonSerializer.Serialize(writer, value.Options, options);
            }
            
            // Write validation if present
            if (value.Validation != null)
            {
                writer.WritePropertyName("validation");
                JsonSerializer.Serialize(writer, value.Validation, options);
            }
            
            writer.WriteEndObject();
        }

        private string? ReadValueBasedOnType(ref Utf8JsonReader reader, TipoInput tipoInput)
        {
            return tipoInput switch
            {
                TipoInput.TextoCorto or TipoInput.TextoLargo or TipoInput.Combobox => 
                    reader.GetString(),
                
                TipoInput.Date => 
                    reader.TryGetDateTime(out var date) ? date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : reader.GetString(),
                
                TipoInput.Number => 
                    reader.TryGetDecimal(out var number) ? number.ToString() : reader.GetString(),
                
                TipoInput.MultipleCheckbox => 
                    JsonSerializer.Serialize(JsonDocument.ParseValue(ref reader).RootElement),
                
                TipoInput.Archivo => 
                    JsonSerializer.Serialize(JsonDocument.ParseValue(ref reader).RootElement),
                
                _ => reader.GetString()
            };
        }

        private void WriteValueBasedOnType(Utf8JsonWriter writer, string propertyName, object? value, TipoInput tipoInput, JsonSerializerOptions options)
        {
            writer.WritePropertyName(propertyName);
            
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            switch (tipoInput)
            {
                case TipoInput.TextoCorto:
                case TipoInput.TextoLargo:
                case TipoInput.Combobox:
                    writer.WriteStringValue(value.ToString());
                    break;
                
                case TipoInput.Date:
                    if (value is DateTime dateTime)
                        writer.WriteStringValue(dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
                    else
                        writer.WriteStringValue(value.ToString());
                    break;
                
                case TipoInput.Number:
                    if (value is decimal decimalValue)
                        writer.WriteNumberValue(decimalValue);
                    else if (decimal.TryParse(value.ToString(), out var parsedDecimal))
                        writer.WriteNumberValue(parsedDecimal);
                    else
                        writer.WriteStringValue(value.ToString());
                    break;
                
                case TipoInput.MultipleCheckbox:
                    if (value is List<string> stringList)
                        JsonSerializer.Serialize(writer, stringList, options);
                    else
                        writer.WriteStringValue(value.ToString());
                    break;
                
                case TipoInput.Archivo:
                    if (value is FileInfoDto fileInfo)
                        JsonSerializer.Serialize(writer, fileInfo, options);
                    else
                        writer.WriteStringValue(value.ToString());
                    break;
                
                default:
                    writer.WriteStringValue(value.ToString());
                    break;
            }
        }
    }
}
