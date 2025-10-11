using System.Collections.Generic;

namespace FluentisCore.DTO
{
    public class GrupoAprobacionCreateDto
    {
        public string? Nombre { get; set; }
        public bool? EsGlobal { get; set; }
        public List<int>? UsuarioIds { get; set; }
    }

    public class GrupoAprobacionUpdateDto
    {
        public string? Nombre { get; set; }
        public bool? EsGlobal { get; set; }
    }

    // Lightweight DTOs for output (avoid EF cycles)
    public class GrupoAprobacionUsuarioDto
    {
        public int IdUsuario { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
        public SimpleRefDto? Rol { get; set; }
        public SimpleRefDto? Departamento { get; set; }
        public SimpleRefDto? Cargo { get; set; }
    }

    public class GrupoAprobacionRelacionUsuarioDto
    {
        public int IdRelacion { get; set; }
        public int GrupoAprobacionId { get; set; }
        public int UsuarioId { get; set; }
        public GrupoAprobacionUsuarioDto? Usuario { get; set; }
    }

    public class GrupoAprobacionListDto
    {
        public int IdGrupo { get; set; }
        public string? Nombre { get; set; }
        public System.DateTime Fecha { get; set; }
        public bool EsGlobal { get; set; }
        public List<GrupoAprobacionRelacionUsuarioDto>? RelacionesUsuarioGrupo { get; set; }
    }

    public class SimpleRefDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
    }
}