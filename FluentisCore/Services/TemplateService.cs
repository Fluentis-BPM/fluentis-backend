using FluentisCore.DTO;
using FluentisCore.Extensions;
using FluentisCore.Models;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.Models.TemplateManagement;
using FluentisCore.Models.WorkflowManagement;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FluentisCore.Services
{
    public interface ITemplateService
    {
        Task<List<PlantillaSolicitudDto>> GetAllAsync();
        Task<PlantillaSolicitudDto?> GetByIdAsync(int id);
        Task<PlantillaSolicitudDto> CreateAsync(PlantillaSolicitudCreateDto dto);
        Task<PlantillaSolicitudDto?> UpdateAsync(int id, PlantillaSolicitudUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<Solicitud> InstanciarSolicitudAsync(InstanciarSolicitudDesdePlantillaDto dto);
    }

    public class TemplateService : ITemplateService
    {
        private readonly FluentisContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TemplateService(FluentisContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<PlantillaSolicitudDto>> GetAllAsync()
        {
            var list = await _context.PlantillasSolicitud
                .Include(p => p.Inputs)
                .ToListAsync();
            return list.Select(p => p.ToDto()).ToList();
        }

        public async Task<PlantillaSolicitudDto?> GetByIdAsync(int id)
        {
            var model = await _context.PlantillasSolicitud
                .Include(p => p.Inputs)
                .FirstOrDefaultAsync(p => p.IdPlantilla == id);
            return model?.ToDto();
        }

        public async Task<PlantillaSolicitudDto> CreateAsync(PlantillaSolicitudCreateDto dto)
        {
            Console.WriteLine("[TemplateService] CreateAsync recibido payload: " + System.Text.Json.JsonSerializer.Serialize(dto));
            // Validaciones previas para evitar errores de FK en SaveChanges
            if (dto.GrupoAprobacionId.HasValue)
            {
                var existsGrupo = await _context.GruposAprobacion.AnyAsync(g => g.IdGrupo == dto.GrupoAprobacionId.Value);
                if (!existsGrupo)
                {
                    Console.WriteLine($"[TemplateService] GrupoAprobacionId inexistente: {dto.GrupoAprobacionId.Value}");
                    throw new InvalidOperationException($"GrupoAprobacionId {dto.GrupoAprobacionId.Value} no existe");
                }
            }

            if (dto.FlujoBaseId.HasValue)
            {
                var existsFlujo = await _context.FlujosAprobacion.AnyAsync(f => f.IdFlujo == dto.FlujoBaseId.Value);
                if (!existsFlujo)
                {
                    Console.WriteLine($"[TemplateService] FlujoBaseId inexistente: {dto.FlujoBaseId.Value}");
                    throw new InvalidOperationException($"FlujoBaseId {dto.FlujoBaseId.Value} no existe");
                }
            }

            if (dto.Inputs is { Count: > 0 })
            {
                var inputIds = dto.Inputs.Select(i => i.InputId).Distinct().ToList();
                var existentes = await _context.Inputs.Where(i => inputIds.Contains(i.IdInput)).Select(i => i.IdInput).ToListAsync();
                var faltantes = inputIds.Except(existentes).ToList();
                if (faltantes.Count > 0)
                {
                    Console.WriteLine($"[TemplateService] Inputs faltantes: {string.Join(", ", faltantes)}");
                    throw new InvalidOperationException($"Algunos InputId no existen: {string.Join(", ", faltantes)}");
                }
            }

            var plantilla = new PlantillaSolicitud
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                FlujoBaseId = dto.FlujoBaseId,
                GrupoAprobacionId = dto.GrupoAprobacionId,
                FechaCreacion = DateTime.UtcNow
            };

            if (dto.Inputs != null && dto.Inputs.Any())
            {
                foreach (var i in dto.Inputs)
                {
                    plantilla.Inputs.Add(new PlantillaInput
                    {
                        InputId = i.InputId,
                        Nombre = i.Nombre,
                        PlaceHolder = i.PlaceHolder,
                        Requerido = i.Requerido,
                        ValorPorDefecto = i.ValorPorDefecto,
                        OpcionesJson = (i.Opciones != null && i.Opciones.Any()) ? JsonSerializer.Serialize(i.Opciones) : null
                    });
                }
            }

            _context.PlantillasSolicitud.Add(plantilla);
            await _context.SaveChangesAsync();
            return plantilla.ToDto();
        }

        public async Task<PlantillaSolicitudDto?> UpdateAsync(int id, PlantillaSolicitudUpdateDto dto)
        {
            Console.WriteLine($"[TemplateService] UpdateAsync id={id} payload: " + System.Text.Json.JsonSerializer.Serialize(dto));
            var plantilla = await _context.PlantillasSolicitud
                .Include(p => p.Inputs)
                .FirstOrDefaultAsync(p => p.IdPlantilla == id);
            if (plantilla == null) return null;

            // Validaciones previas
            if (dto.GrupoAprobacionId.HasValue)
            {
                var existsGrupo = await _context.GruposAprobacion.AnyAsync(g => g.IdGrupo == dto.GrupoAprobacionId.Value);
                if (!existsGrupo)
                    throw new InvalidOperationException($"GrupoAprobacionId {dto.GrupoAprobacionId.Value} no existe");
            }

            if (dto.FlujoBaseId.HasValue)
            {
                var existsFlujo = await _context.FlujosAprobacion.AnyAsync(f => f.IdFlujo == dto.FlujoBaseId.Value);
                if (!existsFlujo)
                    throw new InvalidOperationException($"FlujoBaseId {dto.FlujoBaseId.Value} no existe");
            }

            if (dto.Inputs is { Count: > 0 })
            {
                var inputIds = dto.Inputs.Select(i => i.InputId).Distinct().ToList();
                var existentes = await _context.Inputs.Where(i => inputIds.Contains(i.IdInput)).Select(i => i.IdInput).ToListAsync();
                var faltantes = inputIds.Except(existentes).ToList();
                if (faltantes.Count > 0)
                    throw new InvalidOperationException($"Algunos InputId no existen: {string.Join(", ", faltantes)}");
            }

            if (!string.IsNullOrWhiteSpace(dto.Nombre)) plantilla.Nombre = dto.Nombre;
            if (dto.Descripcion != null) plantilla.Descripcion = dto.Descripcion;
            if (dto.FlujoBaseId.HasValue) plantilla.FlujoBaseId = dto.FlujoBaseId;
            if (dto.GrupoAprobacionId.HasValue) plantilla.GrupoAprobacionId = dto.GrupoAprobacionId;

            if (dto.Inputs != null)
            {
                // Simple strategy: replace all inputs
                _context.PlantillasInput.RemoveRange(plantilla.Inputs);
                plantilla.Inputs.Clear();
                foreach (var i in dto.Inputs)
                {
                    plantilla.Inputs.Add(new PlantillaInput
                    {
                        InputId = i.InputId,
                        Nombre = i.Nombre,
                        PlaceHolder = i.PlaceHolder,
                        Requerido = i.Requerido,
                        ValorPorDefecto = i.ValorPorDefecto,
                        OpcionesJson = (i.Opciones != null && i.Opciones.Any()) ? JsonSerializer.Serialize(i.Opciones) : null
                    });
                }
            }

            await _context.SaveChangesAsync();
            return plantilla.ToDto();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var plantilla = await _context.PlantillasSolicitud.FindAsync(id);
            if (plantilla == null) return false;
            _context.PlantillasSolicitud.Remove(plantilla);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Solicitud> InstanciarSolicitudAsync(InstanciarSolicitudDesdePlantillaDto dto)
        {
            // Permitir que el servicio derive el SolicitanteId desde el token si no viene en el DTO
            if (dto.SolicitanteId <= 0)
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var oid = user?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                          ?? user?.FindFirst("oid")?.Value;
                if (!string.IsNullOrWhiteSpace(oid))
                {
                    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Oid == oid);
                    if (usuario != null)
                    {
                        dto.SolicitanteId = usuario.IdUsuario;
                    }
                }
            }

            if (dto.SolicitanteId <= 0)
                throw new InvalidOperationException("SolicitanteId inválido");

            var solicitanteExiste = await _context.Usuarios.AnyAsync(u => u.IdUsuario == dto.SolicitanteId);
            if (!solicitanteExiste)
                throw new InvalidOperationException($"SolicitanteId {dto.SolicitanteId} no existe");

            var plantilla = await _context.PlantillasSolicitud
                .Include(p => p.Inputs)
                .ThenInclude(pi => pi.Input)
                .FirstOrDefaultAsync(p => p.IdPlantilla == dto.PlantillaId);
            if (plantilla == null) throw new KeyNotFoundException("Plantilla no encontrada");

            if (plantilla.FlujoBaseId.HasValue)
            {
                var flujoExiste = await _context.FlujosAprobacion.AnyAsync(f => f.IdFlujo == plantilla.FlujoBaseId.Value);
                if (!flujoExiste)
                    throw new InvalidOperationException($"FlujoBaseId {plantilla.FlujoBaseId.Value} no existe");
            }

            if (plantilla.GrupoAprobacionId.HasValue)
            {
                var grupoExiste = await _context.GruposAprobacion.AnyAsync(g => g.IdGrupo == plantilla.GrupoAprobacionId.Value);
                if (!grupoExiste)
                    throw new InvalidOperationException($"GrupoAprobacionId {plantilla.GrupoAprobacionId.Value} no existe");
            }

            var solicitud = new Solicitud
            {
                SolicitanteId = dto.SolicitanteId,
                FlujoBaseId = plantilla.FlujoBaseId,
                Nombre = dto.Nombre ?? plantilla.Nombre,
                Descripcion = dto.Descripcion ?? plantilla.Descripcion ?? string.Empty,
                Estado = EstadoSolicitud.Pendiente,
                FechaCreacion = DateTime.UtcNow
            };

            // Primero guardar la solicitud para obtener su Id
            _context.Solicitudes.Add(solicitud);
            await _context.SaveChangesAsync();

            // Inputs desde plantilla (asignando SolicitudId explícitamente)
            var inputsRelacion = new List<RelacionInput>();
            foreach (var pin in plantilla.Inputs)
            {
                string? valor = pin.ValorPorDefecto;
                if (dto.OverridesValores != null && dto.OverridesValores.TryGetValue(pin.InputId, out var overrideValor))
                {
                    valor = overrideValor;
                }

                inputsRelacion.Add(new RelacionInput
                {
                    SolicitudId = solicitud.IdSolicitud,
                    InputId = pin.InputId,
                    Nombre = pin.Nombre,
                    PlaceHolder = pin.PlaceHolder ?? string.Empty,
                    Requerido = pin.Requerido,
                    Valor = valor ?? string.Empty,
                    OptionsJson = pin.OpcionesJson
                });
            }
            if (inputsRelacion.Count > 0)
            {
                _context.AddRange(inputsRelacion);
            }

            // Grupo de aprobación por defecto
            if (plantilla.GrupoAprobacionId.HasValue)
            {
                var relacionGrupo = new RelacionGrupoAprobacion
                {
                    SolicitudId = solicitud.IdSolicitud,
                    GrupoAprobacionId = plantilla.GrupoAprobacionId.Value
                };
                _context.Add(relacionGrupo);
            }

            await _context.SaveChangesAsync();

            // Reconsultar con Includes para asegurar que las navegaciones necesarias estén materializadas
            var loaded = await _context.Solicitudes
                .Include(s => s.Solicitante)
                .Include(s => s.Inputs)
                    .ThenInclude(ri => ri.Input)
                .Include(s => s.GruposAprobacion)
                    .ThenInclude(rg => rg.Decisiones)
                        .ThenInclude(rd => rd.Usuario)
                .FirstAsync(s => s.IdSolicitud == solicitud.IdSolicitud);

            return loaded;
        }
    }
}
