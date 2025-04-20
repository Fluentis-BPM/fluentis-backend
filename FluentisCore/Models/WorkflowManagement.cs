using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentisCore.Models.UserManagement;

namespace FluentisCore.Models.WorkflowManagement
{
    public enum TipoFlujo { Normal, Bifurcacion, Union }
    public enum TipoPaso { Ejecucion, Aprobacion }
    public enum ReglaAprobacion { Unanime, Individual, Ancla }
    public enum EstadoSolicitud { Aprobado, Rechazado }
    public enum EstadoFlujoActivo { EnCurso, Finalizado, Cancelado }
    public enum EstadoPasoSolicitud { Aprobado, Rechazado, Excepcion }

    public class FlujoAprobacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFlujo { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public string Descripcion { get; set; }

        public int? VersionActual { get; set; }

        public bool EsPlantilla { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }

        public int CreadoPor { get; set; }

        [ForeignKey("CreadoPor")]
        public virtual Usuario Usuario { get; set; }

        public ICollection<PasoFlujo> PasosFlujo { get; set; }
    }

    public class PasoFlujo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPaso { get; set; }

        public int FlujoId { get; set; }

        [ForeignKey("FlujoId")]
        public virtual FlujoAprobacion FlujoAprobacion { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        [Required]
        public TipoFlujo TipoFlujo { get; set; }

        [Required]
        public TipoPaso TipoPaso { get; set; }

        [Required]
        public ReglaAprobacion ReglaAprobacion { get; set; }
    }

    public class CaminoParalelo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCamino { get; set; }

        public int PasoOrigenId { get; set; }

        [ForeignKey("PasoOrigenId")]
        public virtual PasoFlujo PasoOrigen { get; set; }

        public int PasoDestinoId { get; set; }

        [ForeignKey("PasoDestinoId")]
        public virtual PasoFlujo PasoDestino { get; set; }

        public bool EsExcepcion { get; set; }
    }

    public class Solicitud
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSolicitud { get; set; }

        public int? FlujoBaseId { get; set; }

        [ForeignKey("FlujoBaseId")]
        public virtual FlujoAprobacion FlujoBase { get; set; }

        public int SolicitanteId { get; set; }

        [ForeignKey("SolicitanteId")]
        public virtual Usuario Solicitante { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }

        public EstadoSolicitud Estado { get; set; }
    }

    public class FlujoActivo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFlujoActivo { get; set; }

        public int SolicitudId { get; set; }

        [ForeignKey("SolicitudId")]
        public virtual Solicitud Solicitud { get; set; }

        public int FlujoEjecucionId { get; set; }

        [ForeignKey("FlujoEjecucionId")]
        public virtual FlujoAprobacion FlujoEjecucion { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFinalizacion { get; set; }

        public EstadoFlujoActivo Estado { get; set; }
    }

    public class PasoSolicitud
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPasoSolicitud { get; set; }

        public int FlujoActivoId { get; set; }

        [ForeignKey("FlujoActivoId")]
        public virtual FlujoActivo FlujoActivo { get; set; }

        public int? PasoId { get; set; }

        [ForeignKey("PasoId")]
        public virtual PasoFlujo PasoFlujo { get; set; }

        public int CaminoId { get; set; }

        [ForeignKey("CaminoId")]
        public virtual CaminoParalelo CaminoParalelo { get; set; }

        public int? ResponsableId { get; set; }

        [ForeignKey("ResponsableId")]
        public virtual Usuario Responsable { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaInicio { get; set; }

        public DateTime? FechaFin { get; set; }

        [Required]
        public TipoPaso TipoPaso { get; set; }

        public EstadoPasoSolicitud Estado { get; set; }

        [StringLength(255)]
        public string Nombre { get; set; }

        [Required]
        public TipoFlujo TipoFlujo { get; set; }

        [Required]
        public ReglaAprobacion ReglaAprobacion { get; set; }
    }
}