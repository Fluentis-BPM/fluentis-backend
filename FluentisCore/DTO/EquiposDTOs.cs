using System.Collections.Generic;

namespace FluentisCore.DTO
{
    public class DepartamentoDto
    {
        public int IdDepartamento { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<UsuarioDto> Usuarios { get; set; } = new List<UsuarioDto>();
    }

    public class RolDto
    {
        public int IdRol { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<UsuarioDto> Usuarios { get; set; } = new List<UsuarioDto>();
    }

    public class CargoDto
    {
        public int IdCargo { get; set; }
        public int? IdJefeCargo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public List<UsuarioDto> Usuarios { get; set; } = new List<UsuarioDto>();
    }
}
