using FluentisCore.DTO;
using FluentisCore.Models.TemplateManagement;

namespace FluentisCore.Extensions
{
    public static class TemplateMappings
    {
        public static PlantillaSolicitudDto ToDto(this PlantillaSolicitud model)
        {
            return new PlantillaSolicitudDto
            {
                IdPlantilla = model.IdPlantilla,
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                FlujoBaseId = model.FlujoBaseId,
                GrupoAprobacionId = model.GrupoAprobacionId,
                FechaCreacion = model.FechaCreacion,
                Inputs = model.Inputs?.Select(i => i.ToDto()).ToList() ?? new()
            };
        }

        public static PlantillaInputDto ToDto(this PlantillaInput model)
        {
            return new PlantillaInputDto
            {
                IdPlantillaInput = model.IdPlantillaInput,
                InputId = model.InputId,
                Nombre = model.Nombre,
                PlaceHolder = model.PlaceHolder,
                Requerido = model.Requerido,
                ValorPorDefecto = model.ValorPorDefecto
            };
        }
    }
}
