using System.Collections.Generic;

namespace FluentisCore.DTO
{
    public class GrupoAprobacionCreateDto
    {
        public string Nombre { get; set; }
        public bool? EsGlobal { get; set; }
        public List<int> UsuarioIds { get; set; }
    }

    public class GrupoAprobacionUpdateDto
    {
        public string Nombre { get; set; }
        public bool? EsGlobal { get; set; }
    }
}