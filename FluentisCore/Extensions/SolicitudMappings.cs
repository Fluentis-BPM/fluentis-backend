using System.Linq;
using FluentisCore.DTO;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.Models.UserManagement;
using FluentisCore.Models.WorkflowManagement;

namespace FluentisCore.Extensions
{
    public static class SolicitudMappings
    {
        public static SolicitudDto ToDto(this Solicitud s)
        {
            return new SolicitudDto
            {
                IdSolicitud = s.IdSolicitud,
                FlujoBaseId = s.FlujoBaseId,
                Nombre = s.Nombre,
                Descripcion = s.Descripcion,
                SolicitanteId = s.SolicitanteId,
                Solicitante = s.Solicitante?.ToMiniDto(),
                FechaCreacion = s.FechaCreacion,
                Estado = s.Estado,
                Inputs = s.Inputs?.Select(i => i.ToDto()).ToList() ?? new(),
                GruposAprobacion = s.GruposAprobacion?.Select(g => g.ToDto()).ToList() ?? new()
            };
        }

        public static UsuarioMiniDto ToMiniDto(this Usuario u)
        {
            return new UsuarioMiniDto
            {
                IdUsuario = u.IdUsuario,
                Nombre = u.Nombre,
                Email = u.Email
            };
        }

        public static RelacionGrupoAprobacionDto ToDto(this RelacionGrupoAprobacion r)
        {
            return new RelacionGrupoAprobacionDto
            {
                IdRelacion = r.IdRelacion,
                GrupoAprobacionId = r.GrupoAprobacionId,
                PasoSolicitudId = r.PasoSolicitudId,
                SolicitudId = r.SolicitudId,
                Decisiones = r.Decisiones?.Select(d => d.ToDto()).ToList() ?? new()
            };
        }

        public static RelacionDecisionUsuarioDto ToDto(this RelacionDecisionUsuario d)
        {
            return new RelacionDecisionUsuarioDto
            {
                IdRelacion = d.IdRelacion,
                IdUsuario = d.IdUsuario,
                Decision = d.Decision,
                FechaDecision = d.FechaDecision,
                Usuario = d.Usuario?.ToMiniDto()
            };
        }
    }
}
