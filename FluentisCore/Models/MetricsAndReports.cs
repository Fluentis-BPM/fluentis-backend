using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentisCore.Models.UserManagement;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.Models.MetricsAndReportsManagement
{
    public enum UnidadMetrica { Segundos, Minutos, Horas, Porcentaje, Numero }
    public enum TipoMetrica { Productividad, Calidad, Eficiencia }
    public enum TipoInforme { Resumen, Detallado, Comparativo, Auditoria }

    public class Metrica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdMetrica { get; set; }

        [StringLength(255)]
        public string Nombre { get; set; }

        public float Valor { get; set; }

        public int FlujoId { get; set; }

        [ForeignKey("FlujoId")]
        public virtual FlujoAprobacion FlujoAprobacion { get; set; }

        public DateTime FechaCalculo { get; set; }

        public string Descripcion { get; set; }

        public UnidadMetrica Unidad { get; set; }

        public float Meta { get; set; }

        public TipoMetrica TipoMetrica { get; set; }
    }

    public class Informe
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInforme { get; set; }

        [StringLength(255)]
        public string Nombre { get; set; }

        public TipoInforme Tipo { get; set; }

        public DateTime FechaGeneracion { get; set; }

        public int UsuarioGeneradorId { get; set; }

        [ForeignKey("UsuarioGeneradorId")]
        public virtual Usuario UsuarioGenerador { get; set; }

        public string Contenido { get; set; }
    }

    public class Excepcion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdExcepcion { get; set; }

        public int PasoSolicitudId { get; set; }

        [ForeignKey("PasoSolicitudId")]
        public virtual PasoSolicitud PasoSolicitud { get; set; }

        public string Motivo { get; set; }

        public DateTime FechaRegistro { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }

    public class InformeMetrica
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInformeMetrica { get; set; }

        public int InformeId { get; set; }

        [ForeignKey("InformeId")]
        public virtual Informe Informe { get; set; }

        public int MetricaId { get; set; }

        [ForeignKey("MetricaId")]
        public virtual Metrica Metrica { get; set; }
    }

    public class InformeFlujo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInformeFlujo { get; set; }

        public int InformeId { get; set; }

        [ForeignKey("InformeId")]
        public virtual Informe Informe { get; set; }

        public int FlujoId { get; set; }

        [ForeignKey("FlujoId")]
        public virtual FlujoAprobacion FlujoAprobacion { get; set; }
    }
}