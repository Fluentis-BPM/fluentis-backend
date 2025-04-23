using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentisCore.Models.UserManagement
{
    public class Departamento
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdDepartamento { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public ICollection<Usuario> Usuarios { get; set; }
    }

    public class Rol
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRol { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        public ICollection<Usuario> Usuarios { get; set; }
    }

    public class Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string Oid { get; set; }

        public int? DepartamentoId { get; set; }

        [ForeignKey("DepartamentoId")]
        public virtual Departamento Departamento { get; set; }

        public int? RolId { get; set; }

        [ForeignKey("RolId")]
        public virtual Rol Rol { get; set; }
    }
}