# Manejo Dinámico de Inputs con DTOs

## Resumen

Este documento explica el sistema mejorado de DTOs para manejar diferentes tipos de input en el Backend de Fluentis. El sistema proporciona serialización/deserialización type-safe para varios tipos de input definidos en el enum `TipoInput`.

## Por qué los DTOs son Esenciales

### 1. **Separación de Contratos de API**
Los DTOs separan tu contrato de API de tus modelos de base de datos, permitiendo:
- Cambiar la estructura de la base de datos sin romper los consumidores de la API
- Controlar exactamente qué datos se exponen
- Prevenir ataques de over-posting
- Agregar validación específica de la API

### 2. **Seguridad de Tipos**
Con tu enum `TipoInput` teniendo múltiples tipos, los DTOs proporcionan:
- Verificación de tipos en tiempo de compilación
- Conversión automática entre almacenamiento de string y valores tipados
- Validación basada en el tipo de input

### 3. **Control de Serialización JSON**
El sistema maneja escenarios de serialización complejos:
- Diferentes tipos de datos (Date, Number, MultipleCheckbox, File)
- Objetos anidados para información de archivos
- Arrays para selecciones múltiples

## Tipos de Input Soportados

```csharp
public enum TipoInput 
{ 
    TextoCorto,      // Texto corto
    TextoLargo,      // Texto largo  
    Combobox,        // Selección única
    MultipleCheckbox,// Selecciones múltiples
    Date,            // Valores de fecha/hora
    Number,          // Valores numéricos
    Archivo          // Subidas de archivos
}
```

## DTOs Principales

### InputValueDto
El DTO principal que maneja la conversión type-safe:

```csharp
public class InputValueDto
{
    public TipoInput TipoInput { get; set; }
    public string? RawValue { get; set; }      // Formato de almacenamiento en base de datos
    public object? Value { get; set; }         // Valor tipado para la API
    public List<string>? Options { get; set; } // Para Combobox/MultipleCheckbox
    public InputValidationDto? Validation { get; set; }
}
```

### Ejemplos JSON

#### Input de Texto
```json
{
  "tipoInput": "TextoCorto",
  "value": "Texto de ejemplo",
  "validation": {
    "required": true,
    "maxLength": 255
  }
}
```

#### Input de Fecha
```json
{
  "tipoInput": "Date",
  "value": "2025-08-07T10:30:00.000Z",
  "validation": {
    "required": true,
    "minDate": "2025-01-01T00:00:00.000Z"
  }
}
```

#### Input Numérico
```json
{
  "tipoInput": "Number",
  "value": 1250.75,
  "validation": {
    "required": true,
    "minValue": 0,
    "maxValue": 10000
  }
}
```

#### Checkbox Múltiple
```json
{
  "tipoInput": "MultipleCheckbox",
  "value": ["Opcion1", "Opcion3", "Opcion5"],
  "options": ["Opcion1", "Opcion2", "Opcion3", "Opcion4", "Opcion5"],
  "validation": {
    "required": true
  }
}
```

#### Subida de Archivo
```json
{
  "tipoInput": "Archivo",
  "value": {
    "fileName": "documento.pdf",
    "contentType": "application/pdf",
    "size": 1048576,
    "filePath": "/uploads/2025/08/documento.pdf",
    "uploadedAt": "2025-08-07T10:30:00.000Z"
  },
  "validation": {
    "required": true,
    "allowedExtensions": [".pdf", ".doc", ".docx"],
    "maxFileSize": 5242880
  }
}
```

## Uso en Controladores

### Creando RelacionInput con valores tipados

```csharp
[HttpPost]
public async Task<ActionResult<RelacionInputDto>> CreateRelacionInput([FromBody] RelacionInputCreateDto dto)
{
    // Validar que el valor del input coincida con su tipo
    if (dto.Valor != null && !dto.Valor.IsValidForType())
    {
        return BadRequest("Valor inválido para el tipo de input especificado");
    }

    var model = dto.ToModel();
    _context.RelacionesInput.Add(model);
    await _context.SaveChangesAsync();

    // Cargar el Input para obtener TipoInput para la respuesta
    await _context.Entry(model).Reference(r => r.Input).LoadAsync();
    
    
    return CreatedAtAction(nameof(GetRelacionInput), 
        new { id = model.IdRelacion }, 
        model.ToDto());
}
```

### Actualizando valores con seguridad de tipos

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateRelacionInput(int id, [FromBody] RelacionInputUpdateDto dto)
{
    var model = await _context.RelacionesInput
        .Include(r => r.Input)
        .FirstOrDefaultAsync(r => r.IdRelacion == id);
    
    if (model == null)
        return NotFound();

    // Validar nuevo valor si se proporciona
    if (dto.Valor != null)
    {
        dto.Valor.TipoInput = model.Input.TipoInput; // Asegurar tipo correcto
        if (!dto.Valor.IsValidForType())
        {
            return BadRequest("Valor inválido para el tipo de input");
        }
    }

    model.UpdateFromDto(dto);
    await _context.SaveChangesAsync();

    return NoContent();
}
```

## Integración con Frontend

### Ejemplo React/TypeScript

```typescript
interface InputValue {
  tipoInput: 'TextoCorto' | 'TextoLargo' | 'Combobox' | 'MultipleCheckbox' | 'Date' | 'Number' | 'Archivo';
  value: any;
  options?: string[];
  validation?: {
    required?: boolean;
    minLength?: number;
    maxLength?: number;
    minValue?: number;
    maxValue?: number;
    minDate?: string;
    maxDate?: string;
    allowedExtensions?: string[];
    maxFileSize?: number;
  };
}

// Renderizado de componente basado en tipo
const renderInput = (inputValue: InputValue) => {
  switch (inputValue.tipoInput) {
    case 'TextoCorto':
      return <input type="text" value={inputValue.value} maxLength={inputValue.validation?.maxLength} />;
    
    case 'Date':
      return <input type="datetime-local" value={inputValue.value} />;
    
    case 'Number':
      return <input type="number" value={inputValue.value} 
                   min={inputValue.validation?.minValue} 
                   max={inputValue.validation?.maxValue} />;
    
    case 'MultipleCheckbox':
      return (
        <div>
          {inputValue.options?.map(option => (
            <label key={option}>
              <input type="checkbox" 
                     checked={inputValue.value?.includes(option)} />
              {option}
            </label>
          ))}
        </div>
      );
    
    case 'Archivo':
      return <input type="file" accept={inputValue.validation?.allowedExtensions?.join(',')} />;
    
    default:
      return <input type="text" value={inputValue.value} />;
  }
};
```

## Beneficios de este Enfoque

1. **Seguridad de Tipos**: Verificación en tiempo de compilación para valores de input
2. **Validación Automática**: Validación incorporada basada en el tipo de input  
3. **API JSON Limpia**: JSON fuertemente tipado con serialización adecuada
4. **Amigable para Frontend**: Fácil de consumir y renderizar diferentes tipos de input
5. **Extensible**: Fácil agregar nuevos tipos de input o reglas de validación
6. **Agnóstico de Base de Datos**: Valores almacenados como strings pero manejados como tipos apropiados

## Guía de Migración

Si tienes registros `RelacionInput` existentes:

1. El campo `Valor` permanece sin cambios (almacenamiento como string)
2. Usar los nuevos DTOs para todas las operaciones de API
3. La conversión ocurre automáticamente vía los métodos de extensión
4. Actualizar frontend para usar la nueva estructura JSON

Este sistema proporciona una base robusta para manejar inputs de formularios complejos mientras mantiene separación limpia entre tu API, lógica de negocio, y capas de almacenamiento de datos.
