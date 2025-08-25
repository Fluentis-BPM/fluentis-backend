using System;
using System.Linq;
using System.Threading.Tasks;
using FluentisCore.Controllers;
using FluentisCore.DTO;
using FluentisCore.Models;
using FluentisCore.Models.InputAndApprovalManagement;
using FluentisCore.Models.UserManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FluentisCore.Tests;

public class ControllerIntegrationTests
{
    private static FluentisContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<FluentisContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? $"TestDb_{Guid.NewGuid()}")
            .Options;
        return new FluentisContext(options);
    }

    [Fact]
    public async Task UsuariosController_CanCreate_And_Get_ById()
    {
        using var context = CreateInMemoryContext();
        var controller = new UsuariosController(context);

        var dto = new UsuarioCreateDto
        {
            Nombre = "Usuario Test",
            Email = $"user_{Guid.NewGuid():N}@example.com",
            Oid = Guid.NewGuid().ToString()
            // DepartamentoId, RolId, CargoId son opcionales
        };

        var createResult = await controller.PostUsuario(dto);

        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdUsuario = Assert.IsType<Usuario>(created.Value);
        Assert.True(createdUsuario.IdUsuario > 0);
        Assert.Equal(dto.Email, createdUsuario.Email);

        var getResult = await controller.GetUsuario(createdUsuario.IdUsuario);
        var fetched = Assert.IsType<ActionResult<UsuarioDto>>(getResult);
        var fetchedValue = Assert.IsType<UsuarioDto>(fetched.Value);
        Assert.Equal(createdUsuario.IdUsuario, fetchedValue.IdUsuario);
        Assert.Equal(dto.Email, fetchedValue.Email);
    }

    [Fact]
    public async Task SolicitudesController_Create_And_Get_Basic()
    {
        using var context = CreateInMemoryContext();

        // Seed a minimal user (solicitante)
        var user = new Usuario
        {
            Nombre = "Solicitante",
            Email = $"solicitante_{Guid.NewGuid():N}@example.com",
            Oid = Guid.NewGuid().ToString()
        };
        context.Usuarios.Add(user);
        await context.SaveChangesAsync();

        var controller = new SolicitudesController(context);

        var createDto = new SolicitudCreateDto
        {
            SolicitanteId = user.IdUsuario,
            Nombre = "Solicitud de Prueba",
            Descripcion = "Desc",
            FlujoBaseId = null,
            Inputs = new(),
            GrupoAprobacionId = null
        };

        var createResult = await controller.CreateSolicitud(createDto);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdSolicitud = Assert.IsType<SolicitudDto>(created.Value);

        Assert.True(createdSolicitud.IdSolicitud > 0);
        Assert.Equal("Solicitud de Prueba", createdSolicitud.Nombre);
        Assert.Equal(Models.WorkflowManagement.EstadoSolicitud.Pendiente, createdSolicitud.Estado);

        var getResult = await controller.GetSolicitud(createdSolicitud.IdSolicitud);
        var fetched = Assert.IsType<ActionResult<SolicitudDto>>(getResult);
        Assert.NotNull(fetched.Value);
        Assert.Equal(createdSolicitud.IdSolicitud, fetched.Value!.IdSolicitud);
    }

    [Fact]
    public async Task SolicitudesController_AddDecision_SetsApproved_AndCreatesFlujoActivo()
    {
        using var context = CreateInMemoryContext();

        // Seed user, approval group, and relation
        var user = new Usuario
        {
            Nombre = "Aprobador",
            Email = $"aprobador_{Guid.NewGuid():N}@example.com",
            Oid = Guid.NewGuid().ToString()
        };
        var group = new GrupoAprobacion
        {
            Nombre = "Grupo Test",
            Fecha = DateTime.UtcNow,
            EsGlobal = false
        };
        context.Usuarios.Add(user);
        context.GruposAprobacion.Add(group);
        await context.SaveChangesAsync();

        // Relacionar usuario con el grupo (único miembro para que 'todos votaron' se cumpla)
        context.RelacionesUsuarioGrupo.Add(new RelacionUsuarioGrupo
        {
            GrupoAprobacionId = group.IdGrupo,
            UsuarioId = user.IdUsuario
        });
        await context.SaveChangesAsync();

        // Crear solicitud con el grupo de aprobación asociado
        var solicitudesController = new SolicitudesController(context);
        var createDto = new SolicitudCreateDto
        {
            SolicitanteId = user.IdUsuario,
            Nombre = "Solicitud con Aprobación",
            Descripcion = "",
            FlujoBaseId = null,
            Inputs = new(),
            GrupoAprobacionId = group.IdGrupo
        };
        var createResult = await solicitudesController.CreateSolicitud(createDto);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdSolicitud = Assert.IsType<SolicitudDto>(created.Value);

        // Emitir decisión de aprobación del único usuario del grupo
        var decisionDto = new RelacionDecisionUsuarioCreateDto
        {
            IdUsuario = user.IdUsuario,
            Decision = true
        };

    var decisionResult = await solicitudesController.AddDecisionToSolicitud(createdSolicitud.IdSolicitud, decisionDto);
    var ok = Assert.IsType<OkObjectResult>(decisionResult);

        // Leer payload en forma segura
        var payload = ok.Value!;
        bool todosVotaron;
        string estadoActual;
        if (payload is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            todosVotaron = je.GetProperty("todosVotaron").GetBoolean();
            estadoActual = je.GetProperty("estadoActual").GetString()!;
        }
        else
        {
            var t = payload.GetType();
            var pTodos = t.GetProperty("TodosVotaron") ?? t.GetProperty("todosVotaron");
            var pEstado = t.GetProperty("EstadoActual") ?? t.GetProperty("estadoActual");
            Assert.NotNull(pTodos);
            Assert.NotNull(pEstado);
            todosVotaron = (bool)pTodos!.GetValue(payload)!;
            estadoActual = pEstado!.GetValue(payload)!.ToString()!;
        }
        Assert.True(todosVotaron);
        Assert.Equal(Models.WorkflowManagement.EstadoSolicitud.Aprobado.ToString(), estadoActual);

        // Verificar que se creó un FlujoActivo automáticamente
        Assert.True(context.FlujosActivos.Any(f => f.SolicitudId == createdSolicitud.IdSolicitud));
    }
}
