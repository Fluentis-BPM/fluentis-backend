using System;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.DTO
{
    /// <summary>
    /// DTO de lectura para exponer información de un FlujoActivo sin filtrar entidades completas de EF.
    /// </summary>
    public class FlujoActivoDto
    {
        public int IdFlujoActivo { get; set; }
        public int SolicitudId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int VersionActual { get; set; }
        public int? FlujoEjecucionId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public EstadoFlujoActivo Estado { get; set; }
        public string? NombreFlujoBase { get; set; }
        public string? EstadoSolicitudOrigen { get; set; }
    }

    /// <summary>
    /// DTO para creación de un FlujoActivo.
    /// </summary>
    public class FlujoActivoCreateDto
    {
        public int SolicitudId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int? FlujoEjecucionId { get; set; }
        public int? VersionActual { get; set; }
        public EstadoFlujoActivo? Estado { get; set; }
    }

    /// <summary>
    /// DTO para actualización parcial de un FlujoActivo.
    /// </summary>
    public class FlujoActivoUpdateDto
    {
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public EstadoFlujoActivo? Estado { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
    }
}
