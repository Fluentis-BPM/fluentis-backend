using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitudTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlantillasSolicitud",
                columns: table => new
                {
                    IdPlantilla = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FlujoBaseId = table.Column<int>(type: "int", nullable: true),
                    GrupoAprobacionId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasSolicitud", x => x.IdPlantilla);
                    table.ForeignKey(
                        name: "FK_PlantillasSolicitud_FlujosAprobacion_FlujoBaseId",
                        column: x => x.FlujoBaseId,
                        principalTable: "FlujosAprobacion",
                        principalColumn: "IdFlujo");
                    table.ForeignKey(
                        name: "FK_PlantillasSolicitud_GruposAprobacion_GrupoAprobacionId",
                        column: x => x.GrupoAprobacionId,
                        principalTable: "GruposAprobacion",
                        principalColumn: "IdGrupo");
                });

            migrationBuilder.CreateTable(
                name: "PlantillasInput",
                columns: table => new
                {
                    IdPlantillaInput = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantillaSolicitudId = table.Column<int>(type: "int", nullable: false),
                    InputId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PlaceHolder = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Requerido = table.Column<bool>(type: "bit", nullable: false),
                    ValorPorDefecto = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasInput", x => x.IdPlantillaInput);
                    table.ForeignKey(
                        name: "FK_PlantillasInput_Inputs_InputId",
                        column: x => x.InputId,
                        principalTable: "Inputs",
                        principalColumn: "IdInput");
                    table.ForeignKey(
                        name: "FK_PlantillasInput_PlantillasSolicitud_PlantillaSolicitudId",
                        column: x => x.PlantillaSolicitudId,
                        principalTable: "PlantillasSolicitud",
                        principalColumn: "IdPlantilla",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasInput_InputId",
                table: "PlantillasInput",
                column: "InputId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasInput_PlantillaSolicitudId",
                table: "PlantillasInput",
                column: "PlantillaSolicitudId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasSolicitud_FlujoBaseId",
                table: "PlantillasSolicitud",
                column: "FlujoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasSolicitud_GrupoAprobacionId",
                table: "PlantillasSolicitud",
                column: "GrupoAprobacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantillasInput");

            migrationBuilder.DropTable(
                name: "PlantillasSolicitud");
        }
    }
}
