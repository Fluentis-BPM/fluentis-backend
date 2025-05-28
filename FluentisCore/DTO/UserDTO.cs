namespace FluentisCore.DTO
{
    public class UserDTO
    {
    }
    public class UsuarioDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public string Oid { get; set; }
        public int? DepartamentoId { get; set; }
        public string DepartamentoNombre { get; set; }
        public int? RolId { get; set; }
        public string RolNombre { get; set; }
        public int? CargoId { get; set; }
        public string CargoNombre { get; set; }
    }
}

