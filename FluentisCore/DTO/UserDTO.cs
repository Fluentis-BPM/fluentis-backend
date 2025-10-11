using System.ComponentModel.DataAnnotations;

namespace FluentisCore.DTO
{
    public class UserDTO
    {
    }
    
    public class UsuarioCreateDto
    {
        [Required]
        [StringLength(255)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Oid { get; set; } = string.Empty;

        public int? DepartamentoId { get; set; }
        public int? RolId { get; set; }
        public int? CargoId { get; set; }
    }
    
    public class UsuarioDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Oid { get; set; } = string.Empty;
        public int? DepartamentoId { get; set; }
        public string? DepartamentoNombre { get; set; }
        public int? RolId { get; set; }
        public string? RolNombre { get; set; }
        public int? CargoId { get; set; }
        public string? CargoNombre { get; set; }
    }
}

