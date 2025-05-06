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

        // Navigation property for related users
        public virtual ICollection<Usuario> Usuarios { get; set; }
    }

    public class Cargo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCargo { get; set; }

        [Required]
        public int IdJefeCargo { get; set; }

        [ForeignKey("IdJefeCargo")]
        public virtual Cargo JefeCargo { get; set; }

        [Required]
        [StringLength(255)]
        public string Nombre { get; set; }

        // Navigation property for related users
        public virtual ICollection<Usuario> Usuarios { get; set; }
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

        // Foreign key to Departamento (assumes Departamento model exists)
        public int? DepartamentoId { get; set; }

        [ForeignKey("DepartamentoId")]
        public virtual Departamento Departamento { get; set; }

        [Required]
        [StringLength(100)]
        public string Oid { get; set; }

        // Foreign key to Rol
        public int? RolId { get; set; }

        [ForeignKey("RolId")]
        public virtual Rol Rol { get; set; }

        // Foreign key to Cargo
        public int? CargoId { get; set; }

        [ForeignKey("CargoId")]
        public virtual Cargo Cargo { get; set; }
    }
}
