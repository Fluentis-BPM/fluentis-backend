# Finalización Automática de Flujos Activos

## Descripción General

El sistema ahora incluye finalización automática de flujos activos mediante un **nodo de tipo Fin** que se crea automáticamente cuando una solicitud es aprobada y se convierte en flujo activo.

## Arquitectura

### 1. Creación Automática del Paso Fin

Cuando una `Solicitud` es aprobada y se convierte en `FlujoActivo`, el sistema crea automáticamente **dos pasos**:

- **Paso Inicio**: Contiene la información inicial de la solicitud mapeada como inputs
- **Paso Fin**: Nodo de control sin responsable que determina cuándo el flujo ha finalizado

```csharp
// En SolicitudController.cs - cuando se aprueba una solicitud
await _workflowInitializationService.CrearPasoInicialAsync(flujoActivo);
await _workflowInitializationService.CrearPasoFinalAsync(flujoActivo);
```

### 2. Características del Paso Fin

```csharp
{
    FlujoActivoId: int,
    ResponsableId: null,           // Sin responsable
    TipoPaso: TipoPaso.Fin,
    Estado: EstadoPasoSolicitud.Pendiente,
    Nombre: "Paso Final",
    TipoFlujo: TipoFlujo.Normal,
    ReglaAprobacion: null,
    FechaInicio: DateTime.UtcNow
}
```

## Lógica de Finalización

### Condiciones para Finalizar un Flujo

Un flujo se considera **finalizado** cuando:

1. Existe un paso de tipo `Fin` en el flujo
2. **TODOS** los pasos que conectan al paso `Fin` (nodos padre) están en estado:
   - `EstadoPasoSolicitud.Aprobado`, O
   - `EstadoPasoSolicitud.Entregado`

### Diagrama de Flujo

```
┌────────────┐
│   Inicio   │
│ (Entregado)│
└─────┬──────┘
      │
      ▼
┌────────────┐     ┌────────────┐
│  Paso 1    │────▶│  Paso 2    │
│ (Aprobado) │     │ (Aprobado) │
└─────┬──────┘     └─────┬──────┘
      │                  │
      └──────┬───────────┘
             │
             ▼
        ┌────────┐
        │  Fin   │ ◄── Se marca como "Entregado" cuando TODOS los padres están completados
        │(Pendiente)│
        └────────┘
```

### Verificación Automática

La verificación ocurre automáticamente en los siguientes momentos:

#### 1. Al Actualizar un Paso (PUT /api/pasosolicitudes/{id})

```csharp
if (paso.Estado == EstadoPasoSolicitud.Aprobado || 
    paso.Estado == EstadoPasoSolicitud.Entregado)
{
    await _workflowService.VerificarYFinalizarFlujoAsync(id);
}
```

#### 2. Al Completar una Votación de Aprobación

```csharp
// En UpdateEstadoPorVotacion()
if (estadoAnterior != EstadoPasoSolicitud.Aprobado && 
    paso.Estado == EstadoPasoSolicitud.Aprobado)
{
    await _workflowService.VerificarYFinalizarFlujoAsync(pasoId);
}
```

## Implementación Técnica

### Método Principal: `VerificarYFinalizarFlujoAsync`

```csharp
public async Task VerificarYFinalizarFlujoAsync(int pasoId)
{
    // 1. Buscar el paso Fin del flujo
    var pasoFin = await _context.PasosSolicitud
        .FirstOrDefaultAsync(p => 
            p.FlujoActivoId == flujoActivoId && 
            p.TipoPaso == TipoPaso.Fin);
    
    // 2. Obtener todos los pasos que conectan AL paso Fin
    var conexionesAlFin = await _context.ConexionesPasoSolicitud
        .Where(c => c.PasoDestinoId == pasoFin.IdPasoSolicitud)
        .Select(c => c.PasoOrigenId)
        .ToListAsync();
    
    // 3. Verificar si TODOS están completados
    var todosCompletados = pasosPadre.All(p => 
        p.Estado == EstadoPasoSolicitud.Aprobado || 
        p.Estado == EstadoPasoSolicitud.Entregado
    );
    
    // 4. Si todos están completados, finalizar
    if (todosCompletados)
    {
        pasoFin.Estado = EstadoPasoSolicitud.Entregado;
        pasoFin.FechaFin = DateTime.UtcNow;
        
        flujoActivo.Estado = EstadoFlujoActivo.Finalizado;
        flujoActivo.FechaFinalizacion = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();
    }
}
```

## Casos de Uso

### Caso 1: Flujo Lineal Simple

```
Inicio → Paso1 → Paso2 → Fin
```

El flujo se finaliza cuando `Paso2` se marca como `Aprobado` o `Entregado`.

### Caso 2: Flujo con Bifurcación y Unión

```
        ┌─→ Paso2 ─┐
Inicio ─┤          ├─→ Fin
        └─→ Paso3 ─┘
```

El flujo se finaliza cuando **AMBOS** `Paso2` Y `Paso3` están en estado `Aprobado` o `Entregado`.

### Caso 3: Flujo Complejo

```
        ┌─→ Paso2 ─┐
Inicio ─┤          ├─→ Paso5 ─→ Fin
        └─→ Paso3 ─┘
```

El flujo se finaliza cuando `Paso5` (que es el único padre del Fin) está completado.

## Estados del Flujo

### Estados de PasoSolicitud
- `Pendiente`: Paso no iniciado
- `Aprobado`: Paso de aprobación aprobado ✅
- `Entregado`: Paso de ejecución completado ✅
- `Rechazado`: Paso rechazado
- `Excepcion`: Paso con excepción
- `Cancelado`: Paso cancelado

### Estados de FlujoActivo
- `EnCurso`: Flujo activo en ejecución
- `Finalizado`: Flujo completado (cuando paso Fin se completa)
- `Cancelado`: Flujo cancelado manualmente

## Consideraciones Importantes

### ⚠️ Importante
1. **El paso Fin debe tener conexiones**: Si no tiene pasos padre conectados, no se finalizará automáticamente
2. **Solo se verifican estados completados**: `Aprobado` y `Entregado` son los únicos estados considerados como "completados"
3. **Verificación no recursiva**: Solo se verifica el paso actualizado, no todo el flujo
4. **Idempotencia**: La verificación puede ejecutarse múltiples veces sin efectos secundarios

### 🔧 Configuración del Flujo
- Conecta todos los pasos finales al paso `Fin` usando `ConexionPasoSolicitud`
- El paso `Fin` no requiere configuración manual, se crea automáticamente
- Puedes tener múltiples caminos que convergen en el paso `Fin`

## Logging

El sistema registra en consola cuando un flujo se finaliza:

```
✅ Flujo 123 finalizado automáticamente
```

## API Endpoints Relacionados

### Actualizar Estado de Paso
```http
PUT /api/pasosolicitudes/{id}
Content-Type: application/json

{
  "estado": "aprobado",
  "fechaFin": "2025-01-15T10:30:00Z"
}
```

### Votar en Paso de Aprobación
```http
POST /api/pasosolicitudes/{id}/decisiones
Content-Type: application/json

{
  "usuarioId": 5,
  "decision": true
}
```

Ambos endpoints disparan automáticamente la verificación de finalización del flujo.

## Ejemplo Completo

```csharp
// 1. Solicitud aprobada → Crea FlujoActivo
var flujoActivo = new FlujoActivo { ... };
await _context.FlujosActivos.Add(flujoActivo);

// 2. Se crean automáticamente Inicio y Fin
await _workflowService.CrearPasoInicialAsync(flujoActivo);  // Estado: Entregado
await _workflowService.CrearPasoFinalAsync(flujoActivo);    // Estado: Pendiente

// 3. Usuario crea pasos intermedios
var paso1 = await CrearPaso(flujoActivo, TipoPaso.Ejecucion);
var paso2 = await CrearPaso(flujoActivo, TipoPaso.Aprobacion);

// 4. Conectar pasos al Fin
await ConectarPasos(paso1.Id, pasoFin.Id);
await ConectarPasos(paso2.Id, pasoFin.Id);

// 5. Completar paso1
paso1.Estado = EstadoPasoSolicitud.Entregado;
await _context.SaveChangesAsync();
// → Verifica finalización (aún no, falta paso2)

// 6. Completar paso2
paso2.Estado = EstadoPasoSolicitud.Aprobado;
await _context.SaveChangesAsync();
// → Verifica finalización → ✅ FLUJO FINALIZADO

// Estado final:
// - pasoFin.Estado = EstadoPasoSolicitud.Entregado
// - flujoActivo.Estado = EstadoFlujoActivo.Finalizado
// - flujoActivo.FechaFinalizacion = DateTime.UtcNow
```

## Testing

Para probar la funcionalidad:

1. Crear una solicitud
2. Aprobar la solicitud (crea flujo con Inicio y Fin)
3. Crear pasos intermedios
4. Conectar pasos al paso Fin
5. Completar los pasos conectados al Fin
6. Verificar que el flujo se marca como `Finalizado`

## Futuras Mejoras

- [ ] Notificaciones cuando un flujo se finaliza
- [ ] Métricas de tiempo de finalización
- [ ] Webhooks para eventos de finalización
- [ ] Dashboard de flujos finalizados
- [ ] Reportes de eficiencia de flujos
