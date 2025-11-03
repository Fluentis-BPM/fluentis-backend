using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRelacionVisualizadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelacionesVisualizadores",
                columns: table => new
                {
                    IdRelacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlujoActivoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelacionesVisualizadores", x => x.IdRelacion);
                    table.ForeignKey(
                        name: "FK_RelacionesVisualizadores_FlujosActivos_FlujoActivoId",
                        column: x => x.FlujoActivoId,
                        principalTable: "FlujosActivos",
                        principalColumn: "IdFlujoActivo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RelacionesVisualizadores_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesVisualizadores_FlujoActivoId",
                table: "RelacionesVisualizadores",
                column: "FlujoActivoId");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesVisualizadores_UsuarioId",
                table: "RelacionesVisualizadores",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelacionesVisualizadores");
        }
    }
}
