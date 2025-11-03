using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentisCore.DTO;
using FluentisCore.Models;
using FluentisCore.Models.WorkflowManagement;
using Microsoft.EntityFrameworkCore;

namespace FluentisCore.Services
{
    public interface IKpiService
    {
        Task<AvgFlowCloseTimeDto> GetAvgFlowCloseTimeAsync(DateTime? startDate, DateTime? endDate, bool includeByFlow);
        Task<AvgStepResponseByTypeDto> GetAvgStepResponseByTypeAsync(DateTime? startDate, DateTime? endDate);
        Task<FlowVolumeByMonthDto> GetFlowVolumeByMonthAsync(int monthsBack);
        Task<BottlenecksDto> GetBottlenecksAsync(DateTime? startDate, DateTime? endDate, string groupBy, int top);
        Task<MonthOverMonthDto> GetMonthOverMonthAsync(string? monthYyyyMm);
        Task<ActiveFlowsSummaryDto> GetActiveFlowsSummaryAsync(DateTime? startDate, DateTime? endDate);
        Task<RequestsSummaryDto> GetRequestsSummaryAsync(DateTime? startDate, DateTime? endDate, int topNUsers);
    }

    public class KpiService : IKpiService
    {
        private readonly FluentisContext _db;

        public KpiService(FluentisContext db)
        {
            _db = db;
        }

        // 1. Tiempo Promedio de Cierre de Flujos
        public async Task<AvgFlowCloseTimeDto> GetAvgFlowCloseTimeAsync(DateTime? startDate, DateTime? endDate, bool includeByFlow)
        {
            var query = _db.FlujosActivos
                .Where(f => f.FechaFinalizacion != null && f.Estado == EstadoFlujoActivo.Finalizado)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(f => f.FechaInicio >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(f => f.FechaInicio <= endDate.Value);

            // Use DateDiffMinute to ensure server-side calculation
            var overallMinutes = await query
                .Select(f => EF.Functions.DateDiffMinute(f.FechaInicio, f.FechaFinalizacion!.Value))
                .Where(diff => diff != null)
                .ToListAsync();

            var result = new AvgFlowCloseTimeDto
            {
                OverallAvgHours = overallMinutes.Any() ? overallMinutes.Average() / 60.0 : 0.0,
                OverallAvgDays = overallMinutes.Any() ? overallMinutes.Average() / 60.0 / 24.0 : 0.0,
                ByFlowName = includeByFlow
                    ? await query
                        .GroupBy(f => f.Nombre)
                        .Select(g => new AvgByFlowDto
                        {
                            FlowName = g.Key,
                            AvgHours = g.Average(f => (double)EF.Functions.DateDiffMinute(f.FechaInicio, f.FechaFinalizacion!.Value)) / 60.0,
                            Count = g.Count()
                        }).ToListAsync()
                    : null
            };

            return result;
        }

        // 2. Tiempo Promedio de Respuesta por Tipo de Paso
        public async Task<AvgStepResponseByTypeDto> GetAvgStepResponseByTypeAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _db.PasosSolicitud
                .Where(p => p.FechaFin != null)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.FechaInicio >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(p => p.FechaInicio <= endDate.Value);

            var grouped = await query
                .GroupBy(p => p.TipoPaso)
                .Select(g => new AvgByStepTypeItem
                {
                    TipoPaso = g.Key == TipoPaso.Aprobacion ? "aprobacion"
                             : g.Key == TipoPaso.Ejecucion ? "ejecucion"
                             : g.Key.ToString().ToLower(),
                    AvgHours = g.Average(p => (double)EF.Functions.DateDiffMinute(p.FechaInicio, p.FechaFin!.Value)) / 60.0,
                    Count = g.Count()
                })
                .ToListAsync();

            // Only keep approbacion and ejecucion in final output
            var items = grouped
                .Where(x => x.TipoPaso == "aprobacion" || x.TipoPaso == "ejecucion")
                .OrderBy(x => x.TipoPaso)
                .ToList();

            return new AvgStepResponseByTypeDto { Items = items };
        }

        // 3. Volumen de Flujos por Mes (últimos N meses)
        public async Task<FlowVolumeByMonthDto> GetFlowVolumeByMonthAsync(int monthsBack)
        {
            var endRef = DateTime.UtcNow;
            var startRef = endRef.AddMonths(-monthsBack + 1); // inclusive

            // Pre-query the data needed
            var iniciados = await _db.FlujosActivos
                .Where(f => f.FechaInicio >= startRef)
                .Select(f => new { f.FechaInicio })
                .ToListAsync();

            var finalizados = await _db.FlujosActivos
                .Where(f => f.FechaFinalizacion != null && f.Estado == EstadoFlujoActivo.Finalizado && f.FechaFinalizacion >= startRef)
                .Select(f => new { FechaFinalizacion = f.FechaFinalizacion!.Value })
                .ToListAsync();

            var cancelados = await _db.FlujosActivos
                .Where(f => f.FechaFinalizacion != null && f.Estado == EstadoFlujoActivo.Cancelado && f.FechaFinalizacion >= startRef)
                .Select(f => new { FechaFinalizacion = f.FechaFinalizacion!.Value })
                .ToListAsync();

            var months = Enumerable.Range(0, monthsBack)
                .Select(i => endRef.AddMonths(-i))
                .Select(d => new DateTime(d.Year, d.Month, 1))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var items = new List<FlowVolumeMonthItem>();
            foreach (var m in months)
            {
                var mEnd = m.AddMonths(1);
                items.Add(new FlowVolumeMonthItem
                {
                    Month = $"{m:yyyy-MM}",
                    Iniciados = iniciados.Count(x => x.FechaInicio >= m && x.FechaInicio < mEnd),
                    Finalizados = finalizados.Count(x => x.FechaFinalizacion >= m && x.FechaFinalizacion < mEnd),
                    Cancelados = cancelados.Count(x => x.FechaFinalizacion >= m && x.FechaFinalizacion < mEnd)
                });
            }

            return new FlowVolumeByMonthDto { Months = items };
        }

        // 4. Cuellos de Botella (top N)
        public async Task<BottlenecksDto> GetBottlenecksAsync(DateTime? startDate, DateTime? endDate, string groupBy, int top)
        {
            groupBy = string.IsNullOrWhiteSpace(groupBy) ? "nombre" : groupBy.ToLower();
            var query = _db.PasosSolicitud
                .Where(p => p.FechaFin != null)
                .AsQueryable();

            if (startDate.HasValue) query = query.Where(p => p.FechaInicio >= startDate.Value);
            if (endDate.HasValue) query = query.Where(p => p.FechaInicio <= endDate.Value);

            IQueryable<BottleneckItem> q;
            if (groupBy == "tipo")
            {
                q = query
                    .GroupBy(p => p.TipoPaso)
                    .Select(g => new BottleneckItem
                    {
                        Key = g.Key == TipoPaso.Aprobacion ? "aprobacion"
                             : g.Key == TipoPaso.Ejecucion ? "ejecucion"
                             : g.Key.ToString().ToLower(),
                        AvgHours = g.Average(p => (double)EF.Functions.DateDiffMinute(p.FechaInicio, p.FechaFin!.Value)) / 60.0,
                        Count = g.Count()
                    });
            }
            else
            {
                q = query
                    .GroupBy(p => p.Nombre)
                    .Select(g => new BottleneckItem
                    {
                        Key = g.Key ?? "(sin nombre)",
                        AvgHours = g.Average(p => (double)EF.Functions.DateDiffMinute(p.FechaInicio, p.FechaFin!.Value)) / 60.0,
                        Count = g.Count()
                    });
            }

            var items = await q
                .OrderByDescending(x => x.AvgHours)
                .ThenByDescending(x => x.Count)
                .Take(top <= 0 ? 10 : top)
                .ToListAsync();

            return new BottlenecksDto { GroupBy = groupBy, Items = items };
        }

        // 5. Comparación Mes vs Mes Anterior
        public async Task<MonthOverMonthDto> GetMonthOverMonthAsync(string? monthYyyyMm)
        {
            // Determine current month range
            DateTime currentMonthStart;
            if (!string.IsNullOrWhiteSpace(monthYyyyMm) && DateTime.TryParse(monthYyyyMm + "-01", out var parsed))
                currentMonthStart = new DateTime(parsed.Year, parsed.Month, 1);
            else
                currentMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var prevMonthStart = currentMonthStart.AddMonths(-1);
            var nextMonthStart = currentMonthStart.AddMonths(1);

            // Metric 1: avg close time hours
            var currentCloseMinutes = await _db.FlujosActivos
                .Where(f => f.Estado == EstadoFlujoActivo.Finalizado && f.FechaFinalizacion != null)
                .Where(f => f.FechaInicio >= currentMonthStart && f.FechaInicio < nextMonthStart)
                .Select(f => EF.Functions.DateDiffMinute(f.FechaInicio, f.FechaFinalizacion!.Value))
                .ToListAsync();
            var prevCloseMinutes = await _db.FlujosActivos
                .Where(f => f.Estado == EstadoFlujoActivo.Finalizado && f.FechaFinalizacion != null)
                .Where(f => f.FechaInicio >= prevMonthStart && f.FechaInicio < currentMonthStart)
                .Select(f => EF.Functions.DateDiffMinute(f.FechaInicio, f.FechaFinalizacion!.Value))
                .ToListAsync();

            double currAvgClose = currentCloseMinutes.Any() ? currentCloseMinutes.Average() / 60.0 : 0.0;
            double prevAvgClose = prevCloseMinutes.Any() ? prevCloseMinutes.Average() / 60.0 : 0.0;

            // Metric 2 & 3: avg approval/execution step hours (by FechaInicio in month)
            async Task<double> StepAvgAsync(DateTime s, DateTime e, TipoPaso tipo)
            {
                var mins = await _db.PasosSolicitud
                    .Where(p => p.TipoPaso == tipo && p.FechaFin != null)
                    .Where(p => p.FechaInicio >= s && p.FechaInicio < e)
                    .Select(p => EF.Functions.DateDiffMinute(p.FechaInicio, p.FechaFin!.Value))
                    .ToListAsync();
                return mins.Any() ? mins.Average() / 60.0 : 0.0;
            }

            var currApr = await StepAvgAsync(currentMonthStart, nextMonthStart, TipoPaso.Aprobacion);
            var prevApr = await StepAvgAsync(prevMonthStart, currentMonthStart, TipoPaso.Aprobacion);
            var currEje = await StepAvgAsync(currentMonthStart, nextMonthStart, TipoPaso.Ejecucion);
            var prevEje = await StepAvgAsync(prevMonthStart, currentMonthStart, TipoPaso.Ejecucion);

            // Metric 4: volumen
            async Task<int> CountFlujosAsync(DateTime s, DateTime e, string kind)
            {
                if (kind == "iniciados") return await _db.FlujosActivos.CountAsync(f => f.FechaInicio >= s && f.FechaInicio < e);
                if (kind == "finalizados") return await _db.FlujosActivos.CountAsync(f => f.Estado == EstadoFlujoActivo.Finalizado && f.FechaFinalizacion != null && f.FechaFinalizacion >= s && f.FechaFinalizacion < e);
                if (kind == "cancelados") return await _db.FlujosActivos.CountAsync(f => f.Estado == EstadoFlujoActivo.Cancelado && f.FechaFinalizacion != null && f.FechaFinalizacion >= s && f.FechaFinalizacion < e);
                return 0;
            }

            var currIni = await CountFlujosAsync(currentMonthStart, nextMonthStart, "iniciados");
            var prevIni = await CountFlujosAsync(prevMonthStart, currentMonthStart, "iniciados");
            var currFin = await CountFlujosAsync(currentMonthStart, nextMonthStart, "finalizados");
            var prevFin = await CountFlujosAsync(prevMonthStart, currentMonthStart, "finalizados");
            var currCan = await CountFlujosAsync(currentMonthStart, nextMonthStart, "cancelados");
            var prevCan = await CountFlujosAsync(prevMonthStart, currentMonthStart, "cancelados");

            double Change(double prev, double curr)
                => prev == 0 ? (curr == 0 ? 0 : 100) : (curr - prev) / prev * 100.0;

            var dto = new MonthOverMonthDto
            {
                Month = $"{currentMonthStart:yyyy-MM}",
                PreviousMonth = $"{prevMonthStart:yyyy-MM}",
                Metrics = new List<MomMetricItem>
                {
                    new MomMetricItem{ Metric = "avg_close_time_hours", Current = currAvgClose, Previous = prevAvgClose, ChangePct = Change(prevAvgClose, currAvgClose) },
                    new MomMetricItem{ Metric = "avg_approval_hours", Current = currApr, Previous = prevApr, ChangePct = Change(prevApr, currApr) },
                    new MomMetricItem{ Metric = "avg_execution_hours", Current = currEje, Previous = prevEje, ChangePct = Change(prevEje, currEje) },
                    new MomMetricItem{ Metric = "flujos_iniciados", Current = currIni, Previous = prevIni, ChangePct = Change(prevIni, currIni) },
                    new MomMetricItem{ Metric = "flujos_finalizados", Current = currFin, Previous = prevFin, ChangePct = Change(prevFin, currFin) },
                    new MomMetricItem{ Metric = "flujos_cancelados", Current = currCan, Previous = prevCan, ChangePct = Change(prevCan, currCan) },
                }
            };

            return dto;
        }

        // 6. Resumen de Flujos Activos
        public async Task<ActiveFlowsSummaryDto> GetActiveFlowsSummaryAsync(DateTime? startDate, DateTime? endDate)
        {
            var qCreated = _db.FlujosActivos.AsQueryable();
            if (startDate.HasValue) qCreated = qCreated.Where(f => f.FechaInicio >= startDate.Value);
            if (endDate.HasValue) qCreated = qCreated.Where(f => f.FechaInicio <= endDate.Value);

            var qFinished = _db.FlujosActivos.Where(f => f.FechaFinalizacion != null && f.Estado == EstadoFlujoActivo.Finalizado).AsQueryable();
            if (startDate.HasValue) qFinished = qFinished.Where(f => f.FechaFinalizacion! >= startDate.Value);
            if (endDate.HasValue) qFinished = qFinished.Where(f => f.FechaFinalizacion! <= endDate.Value);

            var qCanceled = _db.FlujosActivos.Where(f => f.FechaFinalizacion != null && f.Estado == EstadoFlujoActivo.Cancelado).AsQueryable();
            if (startDate.HasValue) qCanceled = qCanceled.Where(f => f.FechaFinalizacion! >= startDate.Value);
            if (endDate.HasValue) qCanceled = qCanceled.Where(f => f.FechaFinalizacion! <= endDate.Value);

            var qInProgress = _db.FlujosActivos.Where(f => f.Estado == EstadoFlujoActivo.EnCurso).AsQueryable();
            if (startDate.HasValue) qInProgress = qInProgress.Where(f => f.FechaInicio >= startDate.Value);
            if (endDate.HasValue) qInProgress = qInProgress.Where(f => f.FechaInicio <= endDate.Value);

            var dto = new ActiveFlowsSummaryDto
            {
                StartDate = startDate,
                EndDate = endDate,
                Creados = await qCreated.CountAsync(),
                Finalizados = await qFinished.CountAsync(),
                EnCurso = await qInProgress.CountAsync(),
                Cancelados = await qCanceled.CountAsync()
            };

            return dto;
        }

        // 7. Resumen de Solicitudes
        public async Task<RequestsSummaryDto> GetRequestsSummaryAsync(DateTime? startDate, DateTime? endDate, int topNUsers)
        {
            var q = _db.Solicitudes.AsQueryable();
            if (startDate.HasValue) q = q.Where(s => s.FechaCreacion >= startDate.Value);
            if (endDate.HasValue) q = q.Where(s => s.FechaCreacion <= endDate.Value);

            var creadas = await q.CountAsync();
            var aprobadas = await q.CountAsync(s => s.Estado == EstadoSolicitud.Aprobado);
            var rechazadas = await q.CountAsync(s => s.Estado == EstadoSolicitud.Rechazado);
            var pendientes = await q.CountAsync(s => s.Estado == EstadoSolicitud.Pendiente);

            // Tiempo promedio de respuesta (aprobaciones): promedio de duración de pasos de aprobación completados en el periodo
            var qSteps = _db.PasosSolicitud.Where(p => p.TipoPaso == TipoPaso.Aprobacion && p.FechaFin != null).AsQueryable();
            if (startDate.HasValue) qSteps = qSteps.Where(p => p.FechaInicio >= startDate.Value);
            if (endDate.HasValue) qSteps = qSteps.Where(p => p.FechaInicio <= endDate.Value);
            var mins = await qSteps.Select(p => EF.Functions.DateDiffMinute(p.FechaInicio, p.FechaFin!.Value)).ToListAsync();
            var avgResp = mins.Any() ? mins.Average() / 60.0 : 0.0;

            // Usuarios más activos: quienes completaron más pasos (FechaFin != null) en el periodo
            var qAllSteps = _db.PasosSolicitud.Where(p => p.FechaFin != null && p.ResponsableId != null).AsQueryable();
            if (startDate.HasValue) qAllSteps = qAllSteps.Where(p => p.FechaInicio >= startDate.Value);
            if (endDate.HasValue) qAllSteps = qAllSteps.Where(p => p.FechaInicio <= endDate.Value);

            var topUsers = await qAllSteps
                .GroupBy(p => p.ResponsableId!.Value)
                .Select(g => new { UsuarioId = g.Key, Pasos = g.Count() })
                .OrderByDescending(x => x.Pasos)
                .Take(topNUsers <= 0 ? 5 : topNUsers)
                .ToListAsync();

            var userIds = topUsers.Select(x => x.UsuarioId).ToList();
            var usersMap = await _db.Usuarios
                .Where(u => userIds.Contains(u.IdUsuario))
                .ToDictionaryAsync(u => u.IdUsuario, u => u.Nombre);

            var dto = new RequestsSummaryDto
            {
                StartDate = startDate,
                EndDate = endDate,
                Creadas = creadas,
                Aprobadas = aprobadas,
                Rechazadas = rechazadas,
                Pendientes = pendientes,
                TiempoPromedioRespuestaAprobacionHoras = avgResp,
                UsuariosMasActivos = topUsers.Select(x => new UsuarioActividadItem
                {
                    UsuarioId = x.UsuarioId,
                    Nombre = usersMap.ContainsKey(x.UsuarioId) ? usersMap[x.UsuarioId] : null,
                    PasosCompletados = x.Pasos
                }).ToList()
            };

            return dto;
        }
    }
}
