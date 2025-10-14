using System.Collections.Generic;
using System.Linq;
using FluentisCore.DTO;
using FluentisCore.Models.WorkflowManagement;
using FluentisCore.Models.InputAndApprovalManagement;

namespace FluentisCore.Extensions
{
    /// <summary>
    /// Métodos de extensión para mapear FlujoActivo <-> DTOs.
    /// </summary>
    public static class WorkflowMappings
    {
        public static FlujoActivoDto ToDto(this FlujoActivo model)
        {
            return new FlujoActivoDto
            {
                IdFlujoActivo = model.IdFlujoActivo,
                SolicitudId = model.SolicitudId,
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                VersionActual = model.VersionActual,
                FlujoEjecucionId = model.FlujoEjecucionId,
                FechaInicio = model.FechaInicio,
                FechaFinalizacion = model.FechaFinalizacion,
                Estado = model.Estado,
                NombreFlujoBase = model.FlujoEjecucion?.Nombre,
                EstadoSolicitudOrigen = model.Solicitud?.Estado.ToString()
            };
        }

        public static FlujoActivo ToModel(this FlujoActivoCreateDto dto)
        {
            return new FlujoActivo
            {
                SolicitudId = dto.SolicitudId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion ?? string.Empty,
                FlujoEjecucionId = dto.FlujoEjecucionId,
                VersionActual = dto.VersionActual ?? 1,
                Estado = dto.Estado ?? EstadoFlujoActivo.EnCurso
            };
        }

        public static void ApplyUpdate(this FlujoActivo model, FlujoActivoUpdateDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Nombre)) model.Nombre = dto.Nombre;
            if (dto.Descripcion != null) model.Descripcion = dto.Descripcion;
            if (dto.Estado.HasValue) model.Estado = dto.Estado.Value;
            if (dto.FechaFinalizacion.HasValue) model.FechaFinalizacion = dto.FechaFinalizacion;
        }

        // ===== Frontend mapping helpers =====

        public static FlujoActivoFrontendDto ToFrontendDto(this FlujoActivo model)
        {
            return new FlujoActivoFrontendDto
            {
                IdFlujoActivo = model.IdFlujoActivo,
                SolicitudId = model.SolicitudId,
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                VersionActual = model.VersionActual,
                FlujoEjecucionId = model.FlujoEjecucionId,
                FechaInicio = model.FechaInicio,
                FechaFinalizacion = model.FechaFinalizacion,
                Estado = MapEstadoFlujo(model.Estado)
            };
        }

        public static PasoSolicitudFrontendDto ToFrontendDto(this PasoSolicitud paso)
        {
            return new PasoSolicitudFrontendDto
            {
                IdPasoSolicitud = paso.IdPasoSolicitud,
                FlujoActivoId = paso.FlujoActivoId,
                PasoId = paso.PasoId,
                CaminoId = paso.CaminoId,
                ResponsableId = paso.ResponsableId,
                FechaInicio = paso.FechaInicio,
                FechaFin = paso.FechaFin,
                TipoPaso = MapTipoPaso(paso.TipoPaso),
                Estado = MapEstadoPaso(paso.Estado),
                Nombre = paso.Nombre,
                TipoFlujo = MapTipoFlujo(paso.TipoFlujo),
                ReglaAprobacion = paso.ReglaAprobacion.HasValue ? MapReglaAprobacion(paso.ReglaAprobacion.Value) : null,
                PosX = paso.PosX,
                PosY = paso.PosY,
                RelacionesInput = paso.RelacionesInput != null ? paso.RelacionesInput.Select(ri => ri.ToFrontendDto()).ToList() : new(),
                RelacionesGrupoAprobacion = paso.RelacionesGrupoAprobacion != null
                    ? new List<RelacionGrupoAprobacionFrontendDto>
                    {
                        new RelacionGrupoAprobacionFrontendDto
                        {
                            IdRelacion = paso.RelacionesGrupoAprobacion.IdRelacion,
                            GrupoAprobacionId = paso.RelacionesGrupoAprobacion.GrupoAprobacionId,
                            PasoSolicitudId = paso.RelacionesGrupoAprobacion.PasoSolicitudId ?? paso.IdPasoSolicitud
                        }
                    }
                    : new List<RelacionGrupoAprobacionFrontendDto>(),
                Comentarios = paso.Comentarios != null ? paso.Comentarios.Select(c => c.ToFrontendDto()).ToList() : new(),
                Excepciones = paso.Excepciones != null ? paso.Excepciones.Select(e => e.ToFrontendDto()).ToList() : new()
            };
        }

        public static RelacionInputFrontendDto ToFrontendDto(this RelacionInput model)
        {
            return new RelacionInputFrontendDto
            {
                IdRelacion = model.IdRelacion,
                InputId = model.InputId,
                Nombre = model.Nombre,
                PlaceHolder = model.PlaceHolder,
                Requerido = model.Requerido,
                Valor = model.Valor,
                TipoInput = MapTipoInput(model.Input?.TipoInput ?? TipoInput.TextoCorto),
                PasoSolicitudId = model.PasoSolicitudId,
                SolicitudId = model.SolicitudId
            };
        }

    public static ComentarioFrontendDto ToFrontendDto(this FluentisCore.Models.CommentAndNotificationManagement.Comentario model)
        {
            return new ComentarioFrontendDto
            {
                IdComentario = model.IdComentario,
                PasoSolicitudId = model.PasoSolicitudId,
                UsuarioId = model.UsuarioId,
                Contenido = model.Contenido,
                FechaCreacion = model.Fecha
            };
        }

    public static ExcepcionFrontendDto ToFrontendDto(this FluentisCore.Models.MetricsAndReportsManagement.Excepcion model)
        {
            return new ExcepcionFrontendDto
            {
                IdExcepcion = model.IdExcepcion,
                PasoSolicitudId = model.PasoSolicitudId,
                Descripcion = model.Motivo,
                FechaRegistro = model.FechaRegistro,
                Estado = "activa"
            };
        }

        public static CaminoParaleloFrontendDto ToFrontendDto(this CaminoParalelo model)
        {
            return new CaminoParaleloFrontendDto
            {
                IdCamino = model.IdCamino,
                PasoOrigenId = model.PasoOrigenId,
                PasoDestinoId = model.PasoDestinoId,
                EsExcepcion = model.EsExcepcion,
                Nombre = null
            };
        }

        private static string MapEstadoFlujo(EstadoFlujoActivo estado)
            => estado switch
            {
                EstadoFlujoActivo.EnCurso => "encurso",
                EstadoFlujoActivo.Finalizado => "finalizado",
                EstadoFlujoActivo.Cancelado => "cancelado",
                _ => "encurso"
            };

        private static string MapTipoPaso(TipoPaso tipo)
            => tipo switch
            {
                TipoPaso.Ejecucion => "ejecucion",
                TipoPaso.Aprobacion => "aprobacion",
                TipoPaso.Inicio => "inicio",
                TipoPaso.Fin => "fin",
                _ => "ejecucion"
            };

        private static string MapEstadoPaso(EstadoPasoSolicitud estado)
            => estado switch
            {
                EstadoPasoSolicitud.Aprobado => "aprobado",
                EstadoPasoSolicitud.Rechazado => "rechazado",
                EstadoPasoSolicitud.Excepcion => "excepcion",
                EstadoPasoSolicitud.Pendiente => "pendiente",
                EstadoPasoSolicitud.Entregado => "entregado",
                EstadoPasoSolicitud.Cancelado => "cancelado",
                _ => "pendiente"
            };

        private static string MapTipoFlujo(TipoFlujo tipo)
            => tipo switch
            {
                TipoFlujo.Normal => "normal",
                TipoFlujo.Bifurcacion => "bifurcacion",
                TipoFlujo.Union => "union",
                _ => "normal"
            };

        // Frontend: 'unanime' | 'individual' | 'ancla'
        private static string MapReglaAprobacion(ReglaAprobacion regla)
            => regla switch
            {
                ReglaAprobacion.Unanimidad => "unanime",
                ReglaAprobacion.PrimeraAprobacion => "individual",
                ReglaAprobacion.Mayoria => "ancla",
                _ => "individual"
            };

        private static string MapTipoInput(TipoInput tipo)
            => tipo switch
            {
                TipoInput.TextoCorto => "texto_corto",
                TipoInput.TextoLargo => "texto_largo",
                TipoInput.Combobox => "combobox",
                TipoInput.MultipleCheckbox => "multiple_checkbox",
                TipoInput.RadioGroup => "radiogroup",
                TipoInput.Date => "date",
                TipoInput.Number => "number",
                TipoInput.Archivo => "archivo",
                _ => "texto_corto"
            };
    }
}
