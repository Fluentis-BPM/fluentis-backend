using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentisCore.Models.UserManagement;

namespace FluentisCore.Models.BackupAndIncidentManagement
{
    public enum TipoBackup { Completo, Incremental, Diferencial, Espejo }
    public enum TipoContenido { Archivo, Enlace }
    public enum SeveridadIncidente { Baja, Media, Alta, Critica }
    public enum EstadoIncidente { EnRevision, Resuelto, Cerrado }

    public class Backup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdBackup { get; set; }

        public DateTime Fecha { get; set; }

        public TipoBackup Tipo { get; set; }

        [StringLength(255)]
        public string Ubicacion { get; set; }

        public TipoContenido TipoContenido { get; set; }

        [Required]
        [StringLength(255)]
        public string ReferenciaContenido { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }
    }

    public class Incidente
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdIncidente { get; set; }

        public string Descripcion { get; set; }

        public SeveridadIncidente Severidad { get; set; }

        public EstadoIncidente Estado { get; set; }

        public int UsuarioReportaId { get; set; }

        [ForeignKey("UsuarioReportaId")]
        public virtual Usuario UsuarioReporta { get; set; }

        public DateTime FechaReporte { get; set; }
    }
}