using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class CreacionModelos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departamentos",
                columns: table => new
                {
                    IdDepartamento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departamentos", x => x.IdDepartamento);
                });

            migrationBuilder.CreateTable(
                name: "GruposAprobacion",
                columns: table => new
                {
                    IdGrupo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EsGlobal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposAprobacion", x => x.IdGrupo);
                });

            migrationBuilder.CreateTable(
                name: "Inputs",
                columns: table => new
                {
                    IdInput = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EsJson = table.Column<bool>(type: "bit", nullable: true),
                    TipoInput = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inputs", x => x.IdInput);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    IdRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DepartamentoId = table.Column<int>(type: "int", nullable: true),
                    RolId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "Departamentos",
                        principalColumn: "IdDepartamento");
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "IdRol");
                });

            migrationBuilder.CreateTable(
                name: "Backups",
                columns: table => new
                {
                    IdBackup = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TipoContenido = table.Column<int>(type: "int", nullable: false),
                    ReferenciaContenido = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Backups", x => x.IdBackup);
                    table.ForeignKey(
                        name: "FK_Backups_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Delegaciones",
                columns: table => new
                {
                    IdRelacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DelegadoId = table.Column<int>(type: "int", nullable: false),
                    SuperiorId = table.Column<int>(type: "int", nullable: false),
                    GrupoAprobacionId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Delegaciones", x => x.IdRelacion);
                    table.ForeignKey(
                        name: "FK_Delegaciones_GruposAprobacion_GrupoAprobacionId",
                        column: x => x.GrupoAprobacionId,
                        principalTable: "GruposAprobacion",
                        principalColumn: "IdGrupo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Delegaciones_Usuarios_DelegadoId",
                        column: x => x.DelegadoId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                    table.ForeignKey(
                        name: "FK_Delegaciones_Usuarios_SuperiorId",
                        column: x => x.SuperiorId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "FlujosAprobacion",
                columns: table => new
                {
                    IdFlujo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionActual = table.Column<int>(type: "int", nullable: true),
                    EsPlantilla = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlujosAprobacion", x => x.IdFlujo);
                    table.ForeignKey(
                        name: "FK_FlujosAprobacion_Usuarios_CreadoPor",
                        column: x => x.CreadoPor,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incidentes",
                columns: table => new
                {
                    IdIncidente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severidad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    UsuarioReportaId = table.Column<int>(type: "int", nullable: false),
                    FechaReporte = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidentes", x => x.IdIncidente);
                    table.ForeignKey(
                        name: "FK_Incidentes_Usuarios_UsuarioReportaId",
                        column: x => x.UsuarioReportaId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Informes",
                columns: table => new
                {
                    IdInforme = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioGeneradorId = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Informes", x => x.IdInforme);
                    table.ForeignKey(
                        name: "FK_Informes_Usuarios_UsuarioGeneradorId",
                        column: x => x.UsuarioGeneradorId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    IdNotificacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    Leida = table.Column<bool>(type: "bit", nullable: false),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.IdNotificacion);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelacionesUsuarioGrupo",
                columns: table => new
                {
                    IdRelacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GrupoAprobacionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelacionesUsuarioGrupo", x => x.IdRelacion);
                    table.ForeignKey(
                        name: "FK_RelacionesUsuarioGrupo_GruposAprobacion_GrupoAprobacionId",
                        column: x => x.GrupoAprobacionId,
                        principalTable: "GruposAprobacion",
                        principalColumn: "IdGrupo");
                    table.ForeignKey(
                        name: "FK_RelacionesUsuarioGrupo_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Metricas",
                columns: table => new
                {
                    IdMetrica = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Valor = table.Column<float>(type: "real", nullable: false),
                    FlujoId = table.Column<int>(type: "int", nullable: false),
                    FechaCalculo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidad = table.Column<int>(type: "int", nullable: false),
                    Meta = table.Column<float>(type: "real", nullable: false),
                    TipoMetrica = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metricas", x => x.IdMetrica);
                    table.ForeignKey(
                        name: "FK_Metricas_FlujosAprobacion_FlujoId",
                        column: x => x.FlujoId,
                        principalTable: "FlujosAprobacion",
                        principalColumn: "IdFlujo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasosFlujo",
                columns: table => new
                {
                    IdPaso = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlujoId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TipoFlujo = table.Column<int>(type: "int", nullable: false),
                    TipoPaso = table.Column<int>(type: "int", nullable: false),
                    ReglaAprobacion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasosFlujo", x => x.IdPaso);
                    table.ForeignKey(
                        name: "FK_PasosFlujo_FlujosAprobacion_FlujoId",
                        column: x => x.FlujoId,
                        principalTable: "FlujosAprobacion",
                        principalColumn: "IdFlujo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Propuestas",
                columns: table => new
                {
                    IdPropuesta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioCreadorId = table.Column<int>(type: "int", nullable: false),
                    FlujoId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propuestas", x => x.IdPropuesta);
                    table.ForeignKey(
                        name: "FK_Propuestas_FlujosAprobacion_FlujoId",
                        column: x => x.FlujoId,
                        principalTable: "FlujosAprobacion",
                        principalColumn: "IdFlujo");
                    table.ForeignKey(
                        name: "FK_Propuestas_Usuarios_UsuarioCreadorId",
                        column: x => x.UsuarioCreadorId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    IdSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlujoBaseId = table.Column<int>(type: "int", nullable: true),
                    SolicitanteId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.IdSolicitud);
                    table.ForeignKey(
                        name: "FK_Solicitudes_FlujosAprobacion_FlujoBaseId",
                        column: x => x.FlujoBaseId,
                        principalTable: "FlujosAprobacion",
                        principalColumn: "IdFlujo");
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InformesFlujo",
                columns: table => new
                {
                    IdInformeFlujo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InformeId = table.Column<int>(type: "int", nullable: false),
                    FlujoId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformesFlujo", x => x.IdInformeFlujo);
                    table.ForeignKey(
                        name: "FK_InformesFlujo_FlujosAprobacion_FlujoId",
                        column: x => x.FlujoId,
                        principalTable: "FlujosAprobacion",
                        principalColumn: "IdFlujo");
                    table.ForeignKey(
                        name: "FK_InformesFlujo_Informes_InformeId",
                        column: x => x.InformeId,
                        principalTable: "Informes",
                        principalColumn: "IdInforme");
                });

            migrationBuilder.CreateTable(
                name: "InformesMetricas",
                columns: table => new
                {
                    IdInformeMetrica = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InformeId = table.Column<int>(type: "int", nullable: false),
                    MetricaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformesMetricas", x => x.IdInformeMetrica);
                    table.ForeignKey(
                        name: "FK_InformesMetricas_Informes_InformeId",
                        column: x => x.InformeId,
                        principalTable: "Informes",
                        principalColumn: "IdInforme");
                    table.ForeignKey(
                        name: "FK_InformesMetricas_Metricas_MetricaId",
                        column: x => x.MetricaId,
                        principalTable: "Metricas",
                        principalColumn: "IdMetrica");
                });

            migrationBuilder.CreateTable(
                name: "CaminosParalelos",
                columns: table => new
                {
                    IdCamino = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasoOrigenId = table.Column<int>(type: "int", nullable: false),
                    PasoDestinoId = table.Column<int>(type: "int", nullable: false),
                    EsExcepcion = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaminosParalelos", x => x.IdCamino);
                    table.ForeignKey(
                        name: "FK_CaminosParalelos_PasosFlujo_PasoDestinoId",
                        column: x => x.PasoDestinoId,
                        principalTable: "PasosFlujo",
                        principalColumn: "IdPaso");
                    table.ForeignKey(
                        name: "FK_CaminosParalelos_PasosFlujo_PasoOrigenId",
                        column: x => x.PasoOrigenId,
                        principalTable: "PasosFlujo",
                        principalColumn: "IdPaso");
                });

            migrationBuilder.CreateTable(
                name: "Votaciones",
                columns: table => new
                {
                    IdVotacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropuestaId = table.Column<int>(type: "int", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votaciones", x => x.IdVotacion);
                    table.ForeignKey(
                        name: "FK_Votaciones_Propuestas_PropuestaId",
                        column: x => x.PropuestaId,
                        principalTable: "Propuestas",
                        principalColumn: "IdPropuesta",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlujosActivos",
                columns: table => new
                {
                    IdFlujoActivo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitudId = table.Column<int>(type: "int", nullable: false),
                    FlujoEjecucionId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinalizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlujosActivos", x => x.IdFlujoActivo);
                    table.ForeignKey(
                        name: "FK_FlujosActivos_FlujosAprobacion_FlujoEjecucionId",
                        column: x => x.FlujoEjecucionId,
                        principalTable: "FlujosAprobacion",
                        principalColumn: "IdFlujo");
                    table.ForeignKey(
                        name: "FK_FlujosActivos_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "IdSolicitud");
                });

            migrationBuilder.CreateTable(
                name: "Votos",
                columns: table => new
                {
                    IdVoto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VotacionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votos", x => x.IdVoto);
                    table.ForeignKey(
                        name: "FK_Votos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Votos_Votaciones_VotacionId",
                        column: x => x.VotacionId,
                        principalTable: "Votaciones",
                        principalColumn: "IdVotacion");
                });

            migrationBuilder.CreateTable(
                name: "PasosSolicitud",
                columns: table => new
                {
                    IdPasoSolicitud = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlujoActivoId = table.Column<int>(type: "int", nullable: false),
                    PasoId = table.Column<int>(type: "int", nullable: true),
                    CaminoId = table.Column<int>(type: "int", nullable: false),
                    ResponsableId = table.Column<int>(type: "int", nullable: true),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TipoPaso = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TipoFlujo = table.Column<int>(type: "int", nullable: false),
                    ReglaAprobacion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasosSolicitud", x => x.IdPasoSolicitud);
                    table.ForeignKey(
                        name: "FK_PasosSolicitud_CaminosParalelos_CaminoId",
                        column: x => x.CaminoId,
                        principalTable: "CaminosParalelos",
                        principalColumn: "IdCamino",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PasosSolicitud_FlujosActivos_FlujoActivoId",
                        column: x => x.FlujoActivoId,
                        principalTable: "FlujosActivos",
                        principalColumn: "IdFlujoActivo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PasosSolicitud_PasosFlujo_PasoId",
                        column: x => x.PasoId,
                        principalTable: "PasosFlujo",
                        principalColumn: "IdPaso");
                    table.ForeignKey(
                        name: "FK_PasosSolicitud_Usuarios_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "Comentarios",
                columns: table => new
                {
                    IdComentario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasoSolicitudId = table.Column<int>(type: "int", nullable: true),
                    FlujoActivoId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comentarios", x => x.IdComentario);
                    table.ForeignKey(
                        name: "FK_Comentarios_FlujosActivos_FlujoActivoId",
                        column: x => x.FlujoActivoId,
                        principalTable: "FlujosActivos",
                        principalColumn: "IdFlujoActivo");
                    table.ForeignKey(
                        name: "FK_Comentarios_PasosSolicitud_PasoSolicitudId",
                        column: x => x.PasoSolicitudId,
                        principalTable: "PasosSolicitud",
                        principalColumn: "IdPasoSolicitud");
                    table.ForeignKey(
                        name: "FK_Comentarios_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Excepciones",
                columns: table => new
                {
                    IdExcepcion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasoSolicitudId = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Excepciones", x => x.IdExcepcion);
                    table.ForeignKey(
                        name: "FK_Excepciones_PasosSolicitud_PasoSolicitudId",
                        column: x => x.PasoSolicitudId,
                        principalTable: "PasosSolicitud",
                        principalColumn: "IdPasoSolicitud",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Excepciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelacionesGrupoAprobacion",
                columns: table => new
                {
                    IdRelacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GrupoAprobacionId = table.Column<int>(type: "int", nullable: false),
                    PasoSolicitudId = table.Column<int>(type: "int", nullable: true),
                    SolicitudId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelacionesGrupoAprobacion", x => x.IdRelacion);
                    table.ForeignKey(
                        name: "FK_RelacionesGrupoAprobacion_GruposAprobacion_GrupoAprobacionId",
                        column: x => x.GrupoAprobacionId,
                        principalTable: "GruposAprobacion",
                        principalColumn: "IdGrupo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RelacionesGrupoAprobacion_PasosSolicitud_PasoSolicitudId",
                        column: x => x.PasoSolicitudId,
                        principalTable: "PasosSolicitud",
                        principalColumn: "IdPasoSolicitud");
                    table.ForeignKey(
                        name: "FK_RelacionesGrupoAprobacion_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "IdSolicitud");
                });

            migrationBuilder.CreateTable(
                name: "RelacionesInput",
                columns: table => new
                {
                    IdRelacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InputId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Requerido = table.Column<bool>(type: "bit", nullable: false),
                    PasoSolicitudId = table.Column<int>(type: "int", nullable: true),
                    SolicitudId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelacionesInput", x => x.IdRelacion);
                    table.ForeignKey(
                        name: "FK_RelacionesInput_Inputs_InputId",
                        column: x => x.InputId,
                        principalTable: "Inputs",
                        principalColumn: "IdInput",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RelacionesInput_PasosSolicitud_PasoSolicitudId",
                        column: x => x.PasoSolicitudId,
                        principalTable: "PasosSolicitud",
                        principalColumn: "IdPasoSolicitud");
                    table.ForeignKey(
                        name: "FK_RelacionesInput_Solicitudes_SolicitudId",
                        column: x => x.SolicitudId,
                        principalTable: "Solicitudes",
                        principalColumn: "IdSolicitud");
                });

            migrationBuilder.CreateTable(
                name: "DecisionesUsuario",
                columns: table => new
                {
                    IdRelacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    RelacionGrupoAprobacionId = table.Column<int>(type: "int", nullable: false),
                    Decision = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionesUsuario", x => x.IdRelacion);
                    table.ForeignKey(
                        name: "FK_DecisionesUsuario_RelacionesGrupoAprobacion_RelacionGrupoAprobacionId",
                        column: x => x.RelacionGrupoAprobacionId,
                        principalTable: "RelacionesGrupoAprobacion",
                        principalColumn: "IdRelacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecisionesUsuario_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Backups_UsuarioId",
                table: "Backups",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_CaminosParalelos_PasoDestinoId",
                table: "CaminosParalelos",
                column: "PasoDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_CaminosParalelos_PasoOrigenId",
                table: "CaminosParalelos",
                column: "PasoOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_FlujoActivoId",
                table: "Comentarios",
                column: "FlujoActivoId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_PasoSolicitudId",
                table: "Comentarios",
                column: "PasoSolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_UsuarioId",
                table: "Comentarios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionesUsuario_IdUsuario",
                table: "DecisionesUsuario",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionesUsuario_RelacionGrupoAprobacionId",
                table: "DecisionesUsuario",
                column: "RelacionGrupoAprobacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegaciones_DelegadoId",
                table: "Delegaciones",
                column: "DelegadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegaciones_GrupoAprobacionId",
                table: "Delegaciones",
                column: "GrupoAprobacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Delegaciones_SuperiorId",
                table: "Delegaciones",
                column: "SuperiorId");

            migrationBuilder.CreateIndex(
                name: "IX_Excepciones_PasoSolicitudId",
                table: "Excepciones",
                column: "PasoSolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_Excepciones_UsuarioId",
                table: "Excepciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_FlujosActivos_FlujoEjecucionId",
                table: "FlujosActivos",
                column: "FlujoEjecucionId");

            migrationBuilder.CreateIndex(
                name: "IX_FlujosActivos_SolicitudId",
                table: "FlujosActivos",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_FlujosAprobacion_CreadoPor",
                table: "FlujosAprobacion",
                column: "CreadoPor");

            migrationBuilder.CreateIndex(
                name: "IX_Incidentes_UsuarioReportaId",
                table: "Incidentes",
                column: "UsuarioReportaId");

            migrationBuilder.CreateIndex(
                name: "IX_Informes_UsuarioGeneradorId",
                table: "Informes",
                column: "UsuarioGeneradorId");

            migrationBuilder.CreateIndex(
                name: "IX_InformesFlujo_FlujoId",
                table: "InformesFlujo",
                column: "FlujoId");

            migrationBuilder.CreateIndex(
                name: "IX_InformesFlujo_InformeId",
                table: "InformesFlujo",
                column: "InformeId");

            migrationBuilder.CreateIndex(
                name: "IX_InformesMetricas_InformeId",
                table: "InformesMetricas",
                column: "InformeId");

            migrationBuilder.CreateIndex(
                name: "IX_InformesMetricas_MetricaId",
                table: "InformesMetricas",
                column: "MetricaId");

            migrationBuilder.CreateIndex(
                name: "IX_Metricas_FlujoId",
                table: "Metricas",
                column: "FlujoId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId",
                table: "Notificaciones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PasosFlujo_FlujoId",
                table: "PasosFlujo",
                column: "FlujoId");

            migrationBuilder.CreateIndex(
                name: "IX_PasosSolicitud_CaminoId",
                table: "PasosSolicitud",
                column: "CaminoId");

            migrationBuilder.CreateIndex(
                name: "IX_PasosSolicitud_FlujoActivoId",
                table: "PasosSolicitud",
                column: "FlujoActivoId");

            migrationBuilder.CreateIndex(
                name: "IX_PasosSolicitud_PasoId",
                table: "PasosSolicitud",
                column: "PasoId");

            migrationBuilder.CreateIndex(
                name: "IX_PasosSolicitud_ResponsableId",
                table: "PasosSolicitud",
                column: "ResponsableId");

            migrationBuilder.CreateIndex(
                name: "IX_Propuestas_FlujoId",
                table: "Propuestas",
                column: "FlujoId");

            migrationBuilder.CreateIndex(
                name: "IX_Propuestas_UsuarioCreadorId",
                table: "Propuestas",
                column: "UsuarioCreadorId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesGrupoAprobacion_GrupoAprobacionId",
                table: "RelacionesGrupoAprobacion",
                column: "GrupoAprobacionId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesGrupoAprobacion_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion",
                column: "PasoSolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesGrupoAprobacion_SolicitudId",
                table: "RelacionesGrupoAprobacion",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesInput_InputId",
                table: "RelacionesInput",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesInput_PasoSolicitudId",
                table: "RelacionesInput",
                column: "PasoSolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesInput_SolicitudId",
                table: "RelacionesInput",
                column: "SolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesUsuarioGrupo_GrupoAprobacionId",
                table: "RelacionesUsuarioGrupo",
                column: "GrupoAprobacionId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesUsuarioGrupo_UsuarioId",
                table: "RelacionesUsuarioGrupo",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_FlujoBaseId",
                table: "Solicitudes",
                column: "FlujoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_SolicitanteId",
                table: "Solicitudes",
                column: "SolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_DepartamentoId",
                table: "Usuarios",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RolId",
                table: "Usuarios",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_Votaciones_PropuestaId",
                table: "Votaciones",
                column: "PropuestaId");

            migrationBuilder.CreateIndex(
                name: "IX_Votos_UsuarioId",
                table: "Votos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Votos_VotacionId",
                table: "Votos",
                column: "VotacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Backups");

            migrationBuilder.DropTable(
                name: "Comentarios");

            migrationBuilder.DropTable(
                name: "DecisionesUsuario");

            migrationBuilder.DropTable(
                name: "Delegaciones");

            migrationBuilder.DropTable(
                name: "Excepciones");

            migrationBuilder.DropTable(
                name: "Incidentes");

            migrationBuilder.DropTable(
                name: "InformesFlujo");

            migrationBuilder.DropTable(
                name: "InformesMetricas");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "RelacionesInput");

            migrationBuilder.DropTable(
                name: "RelacionesUsuarioGrupo");

            migrationBuilder.DropTable(
                name: "Votos");

            migrationBuilder.DropTable(
                name: "RelacionesGrupoAprobacion");

            migrationBuilder.DropTable(
                name: "Informes");

            migrationBuilder.DropTable(
                name: "Metricas");

            migrationBuilder.DropTable(
                name: "Inputs");

            migrationBuilder.DropTable(
                name: "Votaciones");

            migrationBuilder.DropTable(
                name: "GruposAprobacion");

            migrationBuilder.DropTable(
                name: "PasosSolicitud");

            migrationBuilder.DropTable(
                name: "Propuestas");

            migrationBuilder.DropTable(
                name: "CaminosParalelos");

            migrationBuilder.DropTable(
                name: "FlujosActivos");

            migrationBuilder.DropTable(
                name: "PasosFlujo");

            migrationBuilder.DropTable(
                name: "Solicitudes");

            migrationBuilder.DropTable(
                name: "FlujosAprobacion");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Departamentos");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
