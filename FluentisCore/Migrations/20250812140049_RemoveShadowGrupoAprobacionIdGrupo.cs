using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShadowGrupoAprobacionIdGrupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RelacionesUsuarioGrupo_GruposAprobacion_GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo");

            migrationBuilder.DropIndex(
                name: "IX_RelacionesUsuarioGrupo_GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo");

            migrationBuilder.DropColumn(
                name: "GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesUsuarioGrupo_GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo",
                column: "GrupoAprobacionIdGrupo");

            migrationBuilder.AddForeignKey(
                name: "FK_RelacionesUsuarioGrupo_GruposAprobacion_GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo",
                column: "GrupoAprobacionIdGrupo",
                principalTable: "GruposAprobacion",
                principalColumn: "IdGrupo");
        }
    }
}
