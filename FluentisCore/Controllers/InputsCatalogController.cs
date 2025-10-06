using Microsoft.AspNetCore.Mvc;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InputsCatalogController : ControllerBase
{
    /// <summary>
    /// Devuelve el catálogo de tipos de input soportados por la API (nombres oficiales PascalCase)
    /// y algunos alias aceptados para documentación de clientes.
    /// </summary>
    [HttpGet("tipos")]
    public IActionResult GetTipos()
    {
        var oficiales = Enum.GetNames(typeof(TipoInput));
        var response = new
        {
            oficiales,
            alias = new Dictionary<string, string[]>
            {
                ["TextoCorto"] = new[] { "textocorto", "shorttext", "texto_corto" },
                ["TextoLargo"] = new[] { "textolargo", "textarea", "longtext" },
                ["Combobox"] = new[] { "combobox", "select", "dropdown" },
                    ["MultipleCheckbox"] = new[] { "multiplecheckbox", "multiple_checkbox", "multiple-checkbox", "checkboxes", "multicheckbox", "multiopcion", "multiopción" },
                    ["RadioGroup"] = new[] { "radiogroup", "radio", "singlechoice", "opcionunica", "seleccionunica" },
                ["Date"] = new[] { "date", "fecha", "datetime" },
                ["Number"] = new[] { "number", "numeric", "numero" },
                ["Archivo"] = new[] { "archivo", "file", "upload" }
            }
        };
        return Ok(response);
    }
}