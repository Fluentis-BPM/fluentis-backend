using System;
using System.Collections.Generic;

namespace FluentisCore.DTO
{
    // 1. Tiempo Promedio de Cierre de Flujos
    public class AvgFlowCloseTimeDto
    {
        public double OverallAvgHours { get; set; }
        public double OverallAvgDays { get; set; }
        public List<AvgByFlowDto>? ByFlowName { get; set; }
    }

    public class AvgByFlowDto
    {
        public string FlowName { get; set; } = string.Empty;
        public double AvgHours { get; set; }
        public int Count { get; set; }
    }

    // 2. Tiempo Promedio de Respuesta por Tipo de Paso
    public class AvgStepResponseByTypeDto
    {
        public List<AvgByStepTypeItem> Items { get; set; } = new();
    }

    public class AvgByStepTypeItem
    {
        public string TipoPaso { get; set; } = string.Empty; // "aprobacion" | "ejecucion"
        public double AvgHours { get; set; }
        public int Count { get; set; }
    }

    // 3. Volumen de Flujos por Mes
    public class FlowVolumeByMonthDto
    {
        public List<FlowVolumeMonthItem> Months { get; set; } = new();
    }

    public class FlowVolumeMonthItem
    {
        public string Month { get; set; } = string.Empty; // YYYY-MM
        public int Iniciados { get; set; }
        public int Finalizados { get; set; }
        public int Cancelados { get; set; }
    }

    // 4. Cuellos de Botella
    public class BottlenecksDto
    {
        public string GroupBy { get; set; } = "nombre"; // "nombre" | "tipo"
        public List<BottleneckItem> Items { get; set; } = new();
    }

    public class BottleneckItem
    {
        public string Key { get; set; } = string.Empty; // nombre paso o tipo
        public double AvgHours { get; set; }
        public int Count { get; set; }
    }

    // 5. Comparación Mes vs Mes Anterior
    public class MonthOverMonthDto
    {
        public string Month { get; set; } = string.Empty; // YYYY-MM
        public string PreviousMonth { get; set; } = string.Empty; // YYYY-MM
        public List<MomMetricItem> Metrics { get; set; } = new();
    }

    public class MomMetricItem
    {
        public string Metric { get; set; } = string.Empty; // e.g., avg_close_time_hours
        public double Current { get; set; }
        public double Previous { get; set; }
        public double ChangePct { get; set; }
    }

    // 6. Resumen de Flujos Activos
    public class ActiveFlowsSummaryDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Creados { get; set; }
        public int Finalizados { get; set; }
        public int EnCurso { get; set; }
        public int Cancelados { get; set; }
    }

    // 7. Resumen de Solicitudes
    public class RequestsSummaryDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Creadas { get; set; }
        public int Aprobadas { get; set; }
        public int Rechazadas { get; set; }
        public int Pendientes { get; set; }
        public double TiempoPromedioRespuestaAprobacionHoras { get; set; }
        public List<UsuarioActividadItem> UsuariosMasActivos { get; set; } = new();
    }

    public class UsuarioActividadItem
    {
        public int UsuarioId { get; set; }
        public string? Nombre { get; set; }
        public int PasosCompletados { get; set; }
    }
}
