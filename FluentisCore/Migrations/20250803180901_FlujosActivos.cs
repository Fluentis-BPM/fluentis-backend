using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class FlujosActivos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DecisionesUsuario_RelacionesGrupoAprobacion_RelacionGrupoAprobacionId",
                table: "DecisionesUsuario");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Solicitudes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_DecisionesUsuario_RelacionesGrupoAprobacion_RelacionGrupoAprobacionId",
                table: "DecisionesUsuario",
                column: "RelacionGrupoAprobacionId",
                principalTable: "RelacionesGrupoAprobacion",
                principalColumn: "IdRelacion",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DecisionesUsuario_RelacionesGrupoAprobacion_RelacionGrupoAprobacionId",
                table: "DecisionesUsuario");

            migrationBuilder.AlterColumn<int>(
                name: "Estado",
                table: "Solicitudes",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddForeignKey(
                name: "FK_DecisionesUsuario_RelacionesGrupoAprobacion_RelacionGrupoAprobacionId",
                table: "DecisionesUsuario",
                column: "RelacionGrupoAprobacionId",
                principalTable: "RelacionesGrupoAprobacion",
                principalColumn: "IdRelacion",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
