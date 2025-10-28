using System.Threading.Tasks;
using FluentisCore.Models.WorkflowManagement;
using FluentisCore.DTO;
using FluentisCore.Models;
using FluentisCore.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FluentisCore.Services
{
    public interface IWorkflowService
    {
        Task<TipoFlujo> GetTipoFlujo(int pasoId, DbContext context);
        Task<List<FlujoActivoFrontendDto>> GetFlujosByUsuario(
            int usuarioId, 
            DateTime? fechaInicio, 
            DateTime? fechaFin, 
            EstadoFlujoActivo? estado, 
            FluentisContext context);
    }

    public class WorkflowService : IWorkflowService
    {
        public async Task<TipoFlujo> GetTipoFlujo(int pasoId, DbContext context)
        {
            var destinos = await context.Set<CaminoParalelo>()
                .Where(c => c.PasoOrigenId == pasoId)
                .Select(c => c.PasoDestinoId)
                .ToListAsync();

            var origenes = await context.Set<CaminoParalelo>()
                .Where(c => c.PasoDestinoId == pasoId)
                .Select(c => c.PasoOrigenId)
                .ToListAsync();

            if (destinos.Count > 1) return TipoFlujo.Bifurcacion;
            if (origenes.Count > 1) return TipoFlujo.Union;
            return TipoFlujo.Normal;
        }

        public async Task<List<FlujoActivoFrontendDto>> GetFlujosByUsuario(
            int usuarioId,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            EstadoFlujoActivo? estado,
            FluentisContext context)
        {
            // Query base para flujos activos
            var query = context.FlujosActivos
                .Include(f => f.FlujoEjecucion)
                .Include(f => f.Solicitud)
                .AsQueryable();

            // Filtro por fechas
            if (fechaInicio.HasValue)
            {
                query = query.Where(f => f.FechaInicio >= fechaInicio.Value);
            }
            if (fechaFin.HasValue)
            {
                query = query.Where(f => f.FechaInicio <= fechaFin.Value);
            }

            // Filtro por estado
            if (estado.HasValue)
            {
                query = query.Where(f => f.Estado == estado.Value);
            }

            var todosLosFlujos = await query.ToListAsync();

            // Obtener todos los IDs de flujos activos
            var flujoIds = todosLosFlujos.Select(f => f.IdFlujoActivo).ToList();

            // 1. Flujos donde es visualizador
            var flujosComoVisualizador = await context.RelacionesVisualizadores
                .Where(rv => rv.UsuarioId == usuarioId && flujoIds.Contains(rv.FlujoActivoId))
                .Select(rv => rv.FlujoActivoId)
                .Distinct()
                .ToListAsync();

            // 2. Flujos donde es creador (solicitante de la solicitud)
            var flujosComoCreador = await context.Solicitudes
                .Where(s => s.SolicitanteId == usuarioId && flujoIds.Contains(
                    context.FlujosActivos
                        .Where(f => f.SolicitudId == s.IdSolicitud)
                        .Select(f => f.IdFlujoActivo)
                        .FirstOrDefault()
                ))
                .Select(s => context.FlujosActivos
                    .Where(f => f.SolicitudId == s.IdSolicitud)
                    .Select(f => f.IdFlujoActivo)
                    .FirstOrDefault())
                .Distinct()
                .ToListAsync();

            // 3. Flujos donde es ejecutor (responsable de algún paso)
            // Excluir pasos de tipo Inicio y Fin ya que el responsable del Inicio es siempre el creador
            var flujosComoEjecutor = await context.PasosSolicitud
                .Where(ps => ps.ResponsableId.HasValue 
                          && ps.ResponsableId.Value == usuarioId 
                          && flujoIds.Contains(ps.FlujoActivoId)
                          && ps.TipoPaso != TipoPaso.Inicio 
                          && ps.TipoPaso != TipoPaso.Fin)
                .Select(ps => ps.FlujoActivoId)
                .Distinct()
                .ToListAsync();

            // 4. Flujos donde es aprobador (miembro de un grupo de aprobación en algún paso)
            var flujosComoAprobador = await context.RelacionesUsuarioGrupo
                .Where(rug => rug.UsuarioId == usuarioId)
                .Join(context.RelacionesGrupoAprobacion,
                    rug => rug.GrupoAprobacionId,
                    rga => rga.GrupoAprobacionId,
                    (rug, rga) => rga.PasoSolicitudId)
                .Join(context.PasosSolicitud,
                    pasoId => pasoId,
                    paso => paso.IdPasoSolicitud,
                    (pasoId, paso) => paso.FlujoActivoId)
                .Where(flujoId => flujoIds.Contains(flujoId))
                .Distinct()
                .ToListAsync();

            // Combinar todos los flujos únicos donde el usuario participa
            var flujosRelacionados = flujosComoVisualizador
                .Union(flujosComoCreador)
                .Union(flujosComoEjecutor)
                .Union(flujosComoAprobador)
                .Distinct()
                .ToList();

            // Filtrar los flujos y agregar roles
            var resultado = todosLosFlujos
                .Where(f => flujosRelacionados.Contains(f.IdFlujoActivo))
                .Select(f =>
                {
                    var dto = f.ToFrontendDto();
                    dto.RolesUsuario = new List<string>();

                    if (flujosComoVisualizador.Contains(f.IdFlujoActivo))
                        dto.RolesUsuario.Add("visualizador");
                    if (flujosComoCreador.Contains(f.IdFlujoActivo))
                        dto.RolesUsuario.Add("creador");
                    if (flujosComoEjecutor.Contains(f.IdFlujoActivo))
                        dto.RolesUsuario.Add("ejecutor");
                    if (flujosComoAprobador.Contains(f.IdFlujoActivo))
                        dto.RolesUsuario.Add("aprobador");

                    return dto;
                })
                .ToList();

            return resultado;
        }
    }
}