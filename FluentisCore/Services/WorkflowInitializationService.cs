using FluentisCore.Models.WorkflowManagement;
using FluentisCore.Models.InputAndApprovalManagement;
using Microsoft.EntityFrameworkCore;
using FluentisCore.Models;

namespace FluentisCore.Services
{
    public class WorkflowInitializationService
    {
        private readonly FluentisContext _context;

        public WorkflowInitializationService(FluentisContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Crea un paso inicial en el flujo activo con la información de la solicitud mapeada como inputs
        /// </summary>
        /// <param name="flujoActivo">El flujo activo recién creado</param>
        /// <returns>El paso inicial creado</returns>
        public async Task<PasoSolicitud> CrearPasoInicialAsync(FlujoActivo flujoActivo)
        {
            // Cargar la solicitud con sus inputs
            var solicitud = await _context.Solicitudes
                .Include(s => s.Inputs)
                .ThenInclude(ri => ri.Input)
                .FirstOrDefaultAsync(s => s.IdSolicitud == flujoActivo.SolicitudId);

            if (solicitud == null)
            {
                throw new InvalidOperationException($"Solicitud con ID {flujoActivo.SolicitudId} no encontrada");
            }

            // Crear el paso inicial
            var pasoInicial = new PasoSolicitud
            {
                FlujoActivoId = flujoActivo.IdFlujoActivo,
                ResponsableId = solicitud.SolicitanteId, // El solicitante es responsable del paso inicial
                TipoPaso = TipoPaso.Inicio,
                Estado = EstadoPasoSolicitud.Entregado, // El paso inicial ya está "entregado" con la información
                Nombre = "Paso Inicial",
                TipoFlujo = TipoFlujo.Normal,
                ReglaAprobacion = null, // No aplica para paso inicial
                FechaInicio = DateTime.UtcNow,
                FechaFin = DateTime.UtcNow // Se marca como completado inmediatamente
            };

            _context.PasosSolicitud.Add(pasoInicial);
            await _context.SaveChangesAsync();

            // Crear inputs para mapear la información de la solicitud
            await CrearInputsInicialesAsync(pasoInicial, solicitud);

            return pasoInicial;
        }

        /// <summary>
        /// Crea los inputs iniciales mapeando la información de la solicitud
        /// </summary>
        private async Task CrearInputsInicialesAsync(PasoSolicitud pasoInicial, Solicitud solicitud)
        {
            var inputsCreados = new List<RelacionInput>();

            // 1. Crear input para el nombre de la solicitud
            var inputNombre = await CrearInputTextoAsync("Nombre de la Solicitud", solicitud.Nombre, true);
            var relacionNombre = new RelacionInput
            {
                InputId = inputNombre.IdInput,
                Nombre = "Nombre de la Solicitud",
                Valor = solicitud.Nombre,
                PlaceHolder = "Nombre de la solicitud",
                Requerido = true,
                PasoSolicitudId = pasoInicial.IdPasoSolicitud
            };
            inputsCreados.Add(relacionNombre);

            // 2. Crear input para la descripción si existe
            if (!string.IsNullOrEmpty(solicitud.Descripcion))
            {
                var inputDescripcion = await CrearInputTextoLargoAsync("Descripción de la Solicitud", solicitud.Descripcion, false);
                var relacionDescripcion = new RelacionInput
                {
                    InputId = inputDescripcion.IdInput,
                    Nombre = "Descripción de la Solicitud",
                    Valor = solicitud.Descripcion,
                    PlaceHolder = "Descripción de la solicitud",
                    Requerido = false,
                    PasoSolicitudId = pasoInicial.IdPasoSolicitud
                };
                inputsCreados.Add(relacionDescripcion);
            }

            // 3. Mapear los inputs existentes de la solicitud al paso inicial
            if (solicitud.Inputs != null && solicitud.Inputs.Any())
            {
                foreach (var inputOriginal in solicitud.Inputs)
                {
                    var relacionInput = new RelacionInput
                    {
                        InputId = inputOriginal.InputId,
                        Nombre = $"[Original] {inputOriginal.Nombre}",
                        Valor = inputOriginal.Valor,
                        PlaceHolder = inputOriginal.PlaceHolder,
                        Requerido = inputOriginal.Requerido,
                        PasoSolicitudId = pasoInicial.IdPasoSolicitud
                    };
                    inputsCreados.Add(relacionInput);
                }
            }

            // Agregar todos los inputs al contexto
            _context.RelacionesInput.AddRange(inputsCreados);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Crea un input de texto corto
        /// </summary>
        private async Task<Inputs> CrearInputTextoAsync(string nombre, string valor, bool requerido)
        {
            var input = new Inputs
            {
                TipoInput = TipoInput.TextoCorto,
                EsJson = false
            };

            _context.Inputs.Add(input);
            await _context.SaveChangesAsync();
            return input;
        }

        /// <summary>
        /// Crea un input de texto largo
        /// </summary>
        private async Task<Inputs> CrearInputTextoLargoAsync(string nombre, string valor, bool requerido)
        {
            var input = new Inputs
            {
                TipoInput = TipoInput.TextoLargo,
                EsJson = false
            };

            _context.Inputs.Add(input);
            await _context.SaveChangesAsync();
            return input;
        }

        /// <summary>
        /// Crea un paso final para el flujo (opcional, para ser usado cuando se completa el flujo)
        /// </summary>
        public async Task<PasoSolicitud> CrearPasoFinalAsync(int flujoActivoId, int responsableId)
        {
            var pasoFinal = new PasoSolicitud
            {
                FlujoActivoId = flujoActivoId,
                ResponsableId = responsableId,
                TipoPaso = TipoPaso.Fin,
                Estado = EstadoPasoSolicitud.Pendiente,
                Nombre = "Paso Final",
                TipoFlujo = TipoFlujo.Normal,
                ReglaAprobacion = null, // No aplica para paso final
                FechaInicio = DateTime.UtcNow
            };

            _context.PasosSolicitud.Add(pasoFinal);
            await _context.SaveChangesAsync();

            return pasoFinal;
        }
    }
}
