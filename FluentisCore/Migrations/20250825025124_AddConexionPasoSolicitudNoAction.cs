using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class AddConexionPasoSolicitudNoAction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConexionesPasoSolicitud",
                columns: table => new
                {
                    IdConexion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PasoOrigenId = table.Column<int>(type: "int", nullable: false),
                    PasoDestinoId = table.Column<int>(type: "int", nullable: false),
                    EsExcepcion = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConexionesPasoSolicitud", x => x.IdConexion);
                    table.ForeignKey(
                        name: "FK_ConexionesPasoSolicitud_PasosSolicitud_PasoDestinoId",
                        column: x => x.PasoDestinoId,
                        principalTable: "PasosSolicitud",
                        principalColumn: "IdPasoSolicitud");
                    table.ForeignKey(
                        name: "FK_ConexionesPasoSolicitud_PasosSolicitud_PasoOrigenId",
                        column: x => x.PasoOrigenId,
                        principalTable: "PasosSolicitud",
                        principalColumn: "IdPasoSolicitud");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConexionesPasoSolicitud_PasoDestinoId",
                table: "ConexionesPasoSolicitud",
                column: "PasoDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConexionesPasoSolicitud_PasoOrigenId",
                table: "ConexionesPasoSolicitud",
                column: "PasoOrigenId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConexionesPasoSolicitud");
        }
    }
}
