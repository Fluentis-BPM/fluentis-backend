using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentisCore.Models.UserManagement;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.Models.InputAndApprovalManagement
{
    public enum TipoInput { TextoCorto, TextoLargo, Combobox, MultipleCheckbox, Date, Number, Archivo }

    public class Inputs
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInput { get; set; }

        public bool? EsJson { get; set; }

        [Required]
        public TipoInput TipoInput { get; set; }
    }

    public class RelacionInput
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRelacion { get; set; }

        [Required]
        public int InputId { get; set; }

        [ForeignKey("InputId")]
        public virtual Inputs Input { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public string Valor { get; set; }

        [StringLength(255)]
        public string PlaceHolder { get; set; }

        public bool Requerido { get; set; }

        public int? PasoSolicitudId { get; set; }

        [ForeignKey("PasoSolicitudId")]
        public virtual PasoSolicitud PasoSolicitud { get; set; }

        public int? SolicitudId { get; set; }

        [ForeignKey("SolicitudId")]
        public virtual Solicitud Solicitud { get; set; }
    }

    public class GrupoAprobacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdGrupo { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public DateTime Fecha { get; set; }

        public bool EsGlobal { get; set; }

        public virtual ICollection<RelacionUsuarioGrupo> RelacionesUsuarioGrupo { get; set; }

        
    }

    public class RelacionGrupoAprobacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRelacion { get; set; }

        [Required]
        public int GrupoAprobacionId { get; set; }

        [ForeignKey("GrupoAprobacionId")]
        public virtual GrupoAprobacion GrupoAprobacion { get; set; }

        public int? PasoSolicitudId { get; set; }

        [ForeignKey("PasoSolicitudId")]
        public virtual PasoSolicitud PasoSolicitud { get; set; }

        public int? SolicitudId { get; set; }

        [ForeignKey("SolicitudId")]
        public virtual Solicitud Solicitud { get; set; }

        public virtual ICollection<RelacionDecisionUsuario> Decisiones { get; set; }

    }

    public class RelacionDecisionUsuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRelacion { get; set; }

        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        public int RelacionGrupoAprobacionId { get; set; }

        [ForeignKey("RelacionGrupoAprobacionId")]
        public virtual RelacionGrupoAprobacion RelacionGrupoAprobacion { get; set; }

        public bool? Decision { get; set; }

        public DateTime FechaDecision { get; set; }
    }

    public class Delegacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRelacion { get; set; }

        public int DelegadoId { get; set; }

        [ForeignKey("DelegadoId")]
        public virtual Usuario Delegado { get; set; }

        public int SuperiorId { get; set; }

        [ForeignKey("SuperiorId")]
        public virtual Usuario Superior { get; set; }

        public int GrupoAprobacionId { get; set; }

        [ForeignKey("GrupoAprobacionId")]
        public virtual GrupoAprobacion GrupoAprobacion { get; set; }

        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }
    }

    public class RelacionUsuarioGrupo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRelacion { get; set; }

        public int GrupoAprobacionId { get; set; }

        [ForeignKey("GrupoAprobacionId")]
        public virtual GrupoAprobacion GrupoAprobacion { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }

    public class RelacionVisualizador
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRelacion { get; set; }

        [Required]
        public int FlujoActivoId { get; set; }

        [ForeignKey("FlujoActivoId")]
        public virtual FlujoActivo FlujoActivo { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }
}