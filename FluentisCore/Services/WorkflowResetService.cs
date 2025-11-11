using FluentisCore.Models;
using FluentisCore.Models.WorkflowManagement;
using Microsoft.EntityFrameworkCore;

namespace FluentisCore.Services;

/// <summary>
/// Servicio para resetear pasos en caminos de excepción.
/// Cuando se activa un camino de excepción (ej: rechazo), este servicio resetea
/// todos los pasos intermedios entre el origen y el destino del camino.
/// </summary>
public class WorkflowResetService
{
    private readonly FluentisContext _context;

    public WorkflowResetService(FluentisContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Resetea todos los pasos intermedios entre el paso origen y destino de un camino de excepción.
    /// Utiliza BFS (Breadth-First Search) para encontrar todos los pasos en el camino normal
    /// que deben ser reseteados cuando se toma una ruta de excepción.
    /// </summary>
    /// <param name="pasoOrigenId">ID del paso desde donde se origina la excepción</param>
    /// <param name="pasoDestinoId">ID del paso destino de la excepción</param>
    /// <param name="flujoActivoId">ID del flujo activo</param>
    public async Task ResetearPasosIntermediosAsync(int pasoOrigenId, int pasoDestinoId, int flujoActivoId)
    {
        Console.WriteLine($"🔄 Iniciando reset de pasos intermedios entre {pasoOrigenId} y {pasoDestinoId}");

        // 1. Obtener TODOS los pasos del flujo
        var todosPasos = await _context.PasosSolicitud
            .Where(p => p.FlujoActivoId == flujoActivoId)
            .Include(p => p.RelacionesGrupoAprobacion)
                .ThenInclude(r => r.Decisiones)
            .Include(p => p.RelacionesInput)
            .ToListAsync();

        // 2. Obtener TODAS las conexiones del flujo
        var todasConexiones = await _context.ConexionesPasoSolicitud
            .Where(c => todosPasos.Select(p => p.IdPasoSolicitud).Contains(c.PasoOrigenId))
            .ToListAsync();

        // 3. Encontrar todos los pasos "afectados" (entre origen y destino)
        var pasosAResetear = EncontrarPasosIntermedios(
            pasoOrigenId, 
            pasoDestinoId, 
            todosPasos, 
            todasConexiones
        );

        Console.WriteLine($"📋 Pasos a resetear: {pasosAResetear.Count}");
        foreach (var p in pasosAResetear)
        {
            Console.WriteLine($"   - Paso {p.IdPasoSolicitud}: {p.Nombre} (Tipo: {p.TipoPaso}, Estado: {p.Estado})");
        }

        // 4. Resetear cada paso según su tipo
        foreach (var paso in pasosAResetear)
        {
            await ResetearPasoAsync(paso);
        }

        await _context.SaveChangesAsync();
        Console.WriteLine($"✅ Reset completado exitosamente");
    }

    /// <summary>
    /// Encuentra todos los pasos entre origen y destino usando BFS (Breadth-First Search).
    /// Considera múltiples ramas y bifurcaciones del flujo.
    /// Solo explora conexiones NORMALES (no de excepción) para determinar qué resetear.
    /// </summary>
    /// <param name="origenId">ID del paso origen</param>
    /// <param name="destinoId">ID del paso destino</param>
    /// <param name="todosPasos">Lista de todos los pasos del flujo</param>
    /// <param name="todasConexiones">Lista de todas las conexiones del flujo</param>
    /// <returns>Lista de pasos que necesitan ser reseteados</returns>
    private List<PasoSolicitud> EncontrarPasosIntermedios(
        int origenId, 
        int destinoId,
        List<PasoSolicitud> todosPasos,
        List<ConexionPasoSolicitud> todasConexiones)
    {
        var pasosAfectados = new HashSet<int>();
        var visitados = new HashSet<int>();
        var cola = new Queue<int>();

        // Empezar desde las conexiones normales (no de excepción) del origen
        var conexionesNormalesDesdeOrigen = todasConexiones
            .Where(c => c.PasoOrigenId == origenId && !c.EsExcepcion)
            .Select(c => c.PasoDestinoId)
            .ToList();

        Console.WriteLine($"🔍 Explorando desde paso {origenId}, encontradas {conexionesNormalesDesdeOrigen.Count} conexiones normales iniciales");

        foreach (var siguienteId in conexionesNormalesDesdeOrigen)
        {
            cola.Enqueue(siguienteId);
        }

        // BFS para explorar el grafo de flujo
        while (cola.Count > 0)
        {
            var actualId = cola.Dequeue();
            
            // Si llegamos al destino de la excepción, no seguir explorando por esta rama
            if (actualId == destinoId)
            {
                Console.WriteLine($"   ⏸️  Paso {actualId} es el destino, no se incluye en reset");
                continue;
            }

            // Si ya visitamos este nodo, skip para evitar ciclos
            if (visitados.Contains(actualId))
            {
                continue;
            }

            visitados.Add(actualId);
            pasosAfectados.Add(actualId);
            Console.WriteLine($"   ✓ Paso {actualId} marcado para reset");

            // Agregar siguientes pasos (solo conexiones normales, no de excepción)
            var siguientes = todasConexiones
                .Where(c => c.PasoOrigenId == actualId && !c.EsExcepcion)
                .Select(c => c.PasoDestinoId)
                .ToList();

            foreach (var sigId in siguientes)
            {
                if (!visitados.Contains(sigId))
                {
                    cola.Enqueue(sigId);
                }
            }
        }

        // Retornar los objetos PasoSolicitud afectados
        return todosPasos
            .Where(p => pasosAfectados.Contains(p.IdPasoSolicitud))
            .ToList();
    }

    /// <summary>
    /// Resetea un paso individual según su tipo.
    /// </summary>
    /// <param name="paso">Paso a resetear</param>
    private async Task ResetearPasoAsync(PasoSolicitud paso)
    {
        Console.WriteLine($"  🔄 Reseteando paso {paso.IdPasoSolicitud} ({paso.Nombre}) - Tipo: {paso.TipoPaso}");

        switch (paso.TipoPaso)
        {
            case TipoPaso.Ejecucion:
                await ResetearPasoEjecucionAsync(paso);
                break;

            case TipoPaso.Aprobacion:
                await ResetearPasoAprobacionAsync(paso);
                break;

            case TipoPaso.Inicio:
            case TipoPaso.Fin:
                // Inicio y Fin no se resetean
                Console.WriteLine($"  ⏭️  Saltando paso tipo {paso.TipoPaso} (no requiere reset)");
                break;
        }
    }

    /// <summary>
    /// Resetea un paso de Ejecución: borra inputs ingresados y vuelve a Pendiente.
    /// Mantiene: ResponsableId, estructura del paso.
    /// </summary>
    /// <param name="paso">Paso de ejecución a resetear</param>
    private async Task ResetearPasoEjecucionAsync(PasoSolicitud paso)
    {
        // 1. Borrar todos los valores de RelacionesInput (datos ingresados por el usuario)
        if (paso.RelacionesInput?.Any() == true)
        {
            var inputsConValor = paso.RelacionesInput.Where(ri => !string.IsNullOrEmpty(ri.Valor)).ToList();
            if (inputsConValor.Any())
            {
                Console.WriteLine($"    🗑️  Limpiando {inputsConValor.Count} inputs con valores");
                foreach (var input in inputsConValor)
                {
                    input.Valor = string.Empty; // Limpiar el valor pero mantener la estructura
                    _context.Entry(input).State = EntityState.Modified;
                }
            }
        }

        // 2. Resetear estado y fechas
        paso.Estado = EstadoPasoSolicitud.Pendiente;
        paso.FechaFin = null;
        // Mantener FechaInicio, ResponsableId y demás propiedades estructurales

        _context.Entry(paso).State = EntityState.Modified;
        Console.WriteLine($"    ✓ Paso de ejecución reseteado a Pendiente");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Resetea un paso de Aprobación: borra decisiones de usuarios y vuelve a Pendiente.
    /// Mantiene: ResponsableId, GrupoAprobacion asignado, ReglaAprobacion.
    /// </summary>
    /// <param name="paso">Paso de aprobación a resetear</param>
    private async Task ResetearPasoAprobacionAsync(PasoSolicitud paso)
    {
        // 1. Borrar todas las decisiones de usuarios del grupo de aprobación
        if (paso.RelacionesGrupoAprobacion?.Decisiones?.Any() == true)
        {
            var decisiones = paso.RelacionesGrupoAprobacion.Decisiones.ToList();
            Console.WriteLine($"    🗑️  Borrando {decisiones.Count} decisiones de aprobación");
            _context.DecisionesUsuario.RemoveRange(decisiones);
        }

        // 2. Resetear estado y fechas
        paso.Estado = EstadoPasoSolicitud.Pendiente;
        paso.FechaFin = null;
        // Mantener FechaInicio, ResponsableId, RelacionesGrupoAprobacion (solo la relación, no las decisiones)

        _context.Entry(paso).State = EntityState.Modified;
        Console.WriteLine($"    ✓ Paso de aprobación reseteado a Pendiente");
        await Task.CompletedTask;
    }
}
