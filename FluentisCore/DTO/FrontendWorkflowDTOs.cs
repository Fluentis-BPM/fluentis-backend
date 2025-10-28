using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.DTO
{
    // ===== Frontend-aligned DTOs (snake_case + string enums) =====

    public class FlujoActivoFrontendDto
    {
        [JsonPropertyName("id_flujo_activo")] public int IdFlujoActivo { get; set; }
        [JsonPropertyName("solicitud_id")] public int SolicitudId { get; set; }
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("descripcion")] public string? Descripcion { get; set; }
        [JsonPropertyName("version_actual")] public int VersionActual { get; set; }
        [JsonPropertyName("flujo_ejecucion_id")] public int? FlujoEjecucionId { get; set; }
        [JsonPropertyName("fecha_inicio")] public DateTime FechaInicio { get; set; }
        [JsonPropertyName("fecha_finalizacion")] public DateTime? FechaFinalizacion { get; set; }
        // Frontend expects: 'encurso' | 'finalizado' | 'cancelado'
        [JsonPropertyName("estado")] public string Estado { get; set; } = "encurso";

        // Roles del usuario en este flujo (visualizador, aprobador, ejecutor, creador)
        [JsonPropertyName("roles_usuario")] public List<string>? RolesUsuario { get; set; }

        // Optional extra fields for future use
        [JsonPropertyName("datos_solicitud")] public Dictionary<string, string>? DatosSolicitud { get; set; }
        [JsonPropertyName("campos_dinamicos")] public Dictionary<string, string>? CamposDinamicos { get; set; }
    }

    public class RelacionInputFrontendDto
    {
        [JsonPropertyName("id_relacion")] public int? IdRelacion { get; set; }
        [JsonPropertyName("input_id")] public int InputId { get; set; }
        [JsonPropertyName("nombre")] public string Nombre { get; set; } = string.Empty;
        [JsonPropertyName("placeholder")] public string? PlaceHolder { get; set; }
        [JsonPropertyName("requerido")] public bool Requerido { get; set; }
        [JsonPropertyName("valor")] public string? Valor { get; set; }
        [JsonPropertyName("tipo_input")] public string TipoInput { get; set; } = string.Empty;
        [JsonPropertyName("paso_solicitud_id")] public int? PasoSolicitudId { get; set; }
        [JsonPropertyName("solicitud_id")] public int? SolicitudId { get; set; }
        [JsonPropertyName("json_options")] public string? JsonOptions { get; set; }

    }

    public class RelacionGrupoAprobacionFrontendDto
    {
        [JsonPropertyName("id_relacion")] public int IdRelacion { get; set; }
        [JsonPropertyName("grupo_aprobacion_id")] public int GrupoAprobacionId { get; set; }
        [JsonPropertyName("paso_solicitud_id")] public int PasoSolicitudId { get; set; }
    }

    public class ComentarioFrontendDto
    {
        [JsonPropertyName("id_comentario")] public int IdComentario { get; set; }
        [JsonPropertyName("paso_solicitud_id")] public int? PasoSolicitudId { get; set; }
        [JsonPropertyName("usuario_id")] public int UsuarioId { get; set; }
        [JsonPropertyName("contenido")] public string Contenido { get; set; } = string.Empty;
        [JsonPropertyName("fecha_creacion")] public DateTime FechaCreacion { get; set; }
    }

    public class ExcepcionFrontendDto
    {
        [JsonPropertyName("id_excepcion")] public int IdExcepcion { get; set; }
        [JsonPropertyName("paso_solicitud_id")] public int PasoSolicitudId { get; set; }
        [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = string.Empty; // maps from Motivo
        [JsonPropertyName("fecha_registro")] public DateTime FechaRegistro { get; set; }
        // Not present in model; defaulting to 'activa' unless resolved logic is added later
        [JsonPropertyName("estado")] public string Estado { get; set; } = "activa";
    }

    public class CaminoParaleloFrontendDto
    {
        [JsonPropertyName("id_camino")] public int IdCamino { get; set; }
        [JsonPropertyName("paso_origen_id")] public int PasoOrigenId { get; set; }
        [JsonPropertyName("paso_destino_id")] public int PasoDestinoId { get; set; }
        [JsonPropertyName("es_excepcion")] public bool EsExcepcion { get; set; }
        [JsonPropertyName("nombre")] public string? Nombre { get; set; }
    }

    public class PasoSolicitudFrontendDto
    {
        [JsonPropertyName("id_paso_solicitud")] public int IdPasoSolicitud { get; set; }
        [JsonPropertyName("flujo_activo_id")] public int FlujoActivoId { get; set; }
        [JsonPropertyName("paso_id")] public int? PasoId { get; set; }
        [JsonPropertyName("camino_id")] public int? CaminoId { get; set; }
        [JsonPropertyName("responsable_id")] public int? ResponsableId { get; set; }
        [JsonPropertyName("fecha_inicio")] public DateTime FechaInicio { get; set; }
        [JsonPropertyName("fecha_fin")] public DateTime? FechaFin { get; set; }
        [JsonPropertyName("tipo_paso")] public string TipoPaso { get; set; } = "ejecucion"; // 'ejecucion' | 'aprobacion' | 'inicio' | 'fin'
        [JsonPropertyName("estado")] public string Estado { get; set; } = "pendiente";
        [JsonPropertyName("nombre")] public string? Nombre { get; set; }
        [JsonPropertyName("tipo_flujo")] public string TipoFlujo { get; set; } = "normal"; // 'normal' | 'bifurcacion' | 'union'
        [JsonPropertyName("regla_aprobacion")] public string? ReglaAprobacion { get; set; } // 'unanime' | 'individual' | 'ancla'
        [JsonPropertyName("posicion_x")] public int? PosX { get; set; }
        [JsonPropertyName("posicion_y")] public int? PosY { get; set; }
        [JsonPropertyName("relacionesInput")] public List<RelacionInputFrontendDto> RelacionesInput { get; set; } = new();
        [JsonPropertyName("relacionesGrupoAprobacion")] public List<RelacionGrupoAprobacionFrontendDto> RelacionesGrupoAprobacion { get; set; } = new();
        [JsonPropertyName("comentarios")] public List<ComentarioFrontendDto> Comentarios { get; set; } = new();
        [JsonPropertyName("excepciones")] public List<ExcepcionFrontendDto> Excepciones { get; set; } = new();
    }

    public class FlujoActivoResponseDto
    {
        [JsonPropertyName("flujoActivoId")] public int FlujoActivoId { get; set; }
        [JsonPropertyName("pasos")] public List<PasoSolicitudFrontendDto> Pasos { get; set; } = new();
        [JsonPropertyName("caminos")] public List<CaminoParaleloFrontendDto> Caminos { get; set; } = new();
    }
}
