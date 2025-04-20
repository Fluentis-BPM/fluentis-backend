using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FluentisCore.Models.UserManagement;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.Models.ProposalAndVotingManagement
{
    public enum ResultadoVotacion { Aprobado, Rechazado }
    public enum ValorVoto { Aprobado, Rechazado }

    public class Propuesta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdPropuesta { get; set; }

        [Required]
        [StringLength(255)]
        public string Titulo { get; set; }

        public string Descripcion { get; set; }

        public int UsuarioCreadorId { get; set; }

        [ForeignKey("UsuarioCreadorId")]
        public virtual Usuario UsuarioCreador { get; set; }

        public int? FlujoId { get; set; }

        [ForeignKey("FlujoId")]
        public virtual FlujoAprobacion FlujoAprobacion { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime FechaCreacion { get; set; }
    }

    public class Votacion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdVotacion { get; set; }

        public int PropuestaId { get; set; }

        [ForeignKey("PropuestaId")]
        public virtual Propuesta Propuesta { get; set; }

        public ResultadoVotacion? Resultado { get; set; }

        public DateTime? FechaCierre { get; set; }
    }

    public class Voto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdVoto { get; set; }

        public int VotacionId { get; set; }

        [ForeignKey("VotacionId")]
        public virtual Votacion Votacion { get; set; }

        public int UsuarioId { get; set; }

        [ForeignKey("UsuarioId")]
        public virtual Usuario Usuario { get; set; }

        public ValorVoto Valor { get; set; }

        public DateTime Fecha { get; set; }
    }
}