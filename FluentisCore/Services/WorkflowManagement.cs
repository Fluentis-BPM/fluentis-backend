using System.Threading.Tasks;
using FluentisCore.Models.WorkflowManagement;
using Microsoft.EntityFrameworkCore;

namespace FluentisCore.Services
{
    public interface IWorkflowService
    {
        Task<TipoFlujo> GetTipoFlujo(int pasoId, DbContext context);
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
    }
}