using System.Text.Json;
using System.Text.Json.Serialization;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.Converters;

/// <summary>
/// JsonConverter que acepta múltiples alias y formatos (case-insensitive, guiones, underscores)
/// para <see cref="TipoInput"/> y siempre serializa en PascalCase (nombre oficial del enum).
/// Esto hace resiliente la API frente a clientes legacy y facilita evolución futura.
/// </summary>
public class TipoInputJsonConverter : JsonConverter<TipoInput>
{
    private static readonly Dictionary<string, TipoInput> _alias = new(StringComparer.OrdinalIgnoreCase)
    {
        // TextoCorto
        ["textocorto"] = TipoInput.TextoCorto,
        ["texto_corto"] = TipoInput.TextoCorto,
        ["texto-corto"] = TipoInput.TextoCorto,
        ["shorttext"] = TipoInput.TextoCorto,
        ["inputtext"] = TipoInput.TextoCorto,
        ["texto"] = TipoInput.TextoCorto,

        // TextoLargo
        ["textolargo"] = TipoInput.TextoLargo,
        ["texto_largo"] = TipoInput.TextoLargo,
        ["texto-largo"] = TipoInput.TextoLargo,
        ["textarea"] = TipoInput.TextoLargo,
        ["longtext"] = TipoInput.TextoLargo,

        // Combobox
        ["combobox"] = TipoInput.Combobox,
        ["select"] = TipoInput.Combobox,
        ["dropdown"] = TipoInput.Combobox,

        // MultipleCheckbox
        ["multiplecheckbox"] = TipoInput.MultipleCheckbox,
        ["multiple_checkbox"] = TipoInput.MultipleCheckbox,
        ["multiple-checkbox"] = TipoInput.MultipleCheckbox,
        ["checkboxes"] = TipoInput.MultipleCheckbox,
        ["multicheckbox"] = TipoInput.MultipleCheckbox,
        ["multiopcion"] = TipoInput.MultipleCheckbox,
        ["multiopción"] = TipoInput.MultipleCheckbox,
    // RadioGroup (selección única con múltiples opciones)
    ["radiogroup"] = TipoInput.RadioGroup,
    ["radio"] = TipoInput.RadioGroup,
    ["singlechoice"] = TipoInput.RadioGroup,
    ["opcionunica"] = TipoInput.RadioGroup,
    ["seleccionunica"] = TipoInput.RadioGroup,

        // Date
        ["date"] = TipoInput.Date,
        ["fecha"] = TipoInput.Date,
        ["datetime"] = TipoInput.Date,
        ["fechahora"] = TipoInput.Date,

        // Number
        ["number"] = TipoInput.Number,
        ["numeric"] = TipoInput.Number,
        ["numero"] = TipoInput.Number,
        ["número"] = TipoInput.Number,

        // Archivo
        ["archivo"] = TipoInput.Archivo,
        ["file"] = TipoInput.Archivo,
        ["upload"] = TipoInput.Archivo,
        ["documento"] = TipoInput.Archivo,
    };

    public override TipoInput Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString() ?? string.Empty;
            var normalized = raw.Trim().ToLowerInvariant().Replace("-", "").Replace("_", "");

            // Intento directo alias
            if (_alias.TryGetValue(raw, out var direct)) return direct;
            if (_alias.TryGetValue(normalized, out var norm)) return norm;

            // Intento parse nativo (case-insensitive)
            if (Enum.TryParse<TipoInput>(raw, true, out var parsed)) return parsed;

            throw new JsonException($"Valor de TipoInput no reconocido: '{raw}'");
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("TipoInput no puede ser null");
        }

        throw new JsonException($"Token inesperado para TipoInput: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, TipoInput value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString()); // Nombre oficial PascalCase
    }
}