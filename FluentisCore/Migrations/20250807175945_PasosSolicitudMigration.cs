using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class PasosSolicitudMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comentarios_PasosSolicitud_PasoSolicitudId",
                table: "Comentarios");

            migrationBuilder.DropForeignKey(
                name: "FK_PasosSolicitud_CaminosParalelos_CaminoId",
                table: "PasosSolicitud");

            migrationBuilder.DropForeignKey(
                name: "FK_RelacionesGrupoAprobacion_PasosSolicitud_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion");

            migrationBuilder.DropForeignKey(
                name: "FK_RelacionesInput_PasosSolicitud_PasoSolicitudId",
                table: "RelacionesInput");

            migrationBuilder.DropIndex(
                name: "IX_RelacionesGrupoAprobacion_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion");

            migrationBuilder.AddColumn<int>(
                name: "GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ReglaAprobacion",
                table: "PasosSolicitud",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "CaminoId",
                table: "PasosSolicitud",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDecision",
                table: "DecisionesUsuario",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesUsuarioGrupo_GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo",
                column: "GrupoAprobacionIdGrupo");

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesGrupoAprobacion_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion",
                column: "PasoSolicitudId",
                unique: true,
                filter: "[PasoSolicitudId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Comentarios_PasosSolicitud_PasoSolicitudId",
                table: "Comentarios",
                column: "PasoSolicitudId",
                principalTable: "PasosSolicitud",
                principalColumn: "IdPasoSolicitud",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PasosSolicitud_CaminosParalelos_CaminoId",
                table: "PasosSolicitud",
                column: "CaminoId",
                principalTable: "CaminosParalelos",
                principalColumn: "IdCamino");

            migrationBuilder.AddForeignKey(
                name: "FK_RelacionesGrupoAprobacion_PasosSolicitud_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion",
                column: "PasoSolicitudId",
                principalTable: "PasosSolicitud",
                principalColumn: "IdPasoSolicitud",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RelacionesInput_PasosSolicitud_PasoSolicitudId",
                table: "RelacionesInput",
                column: "PasoSolicitudId",
                principalTable: "PasosSolicitud",
                principalColumn: "IdPasoSolicitud",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RelacionesUsuarioGrupo_GruposAprobacion_GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo",
                column: "GrupoAprobacionIdGrupo",
                principalTable: "GruposAprobacion",
                principalColumn: "IdGrupo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comentarios_PasosSolicitud_PasoSolicitudId",
                table: "Comentarios");

            migrationBuilder.DropForeignKey(
                name: "FK_PasosSolicitud_CaminosParalelos_CaminoId",
                table: "PasosSolicitud");

            migrationBuilder.DropForeignKey(
                name: "FK_RelacionesGrupoAprobacion_PasosSolicitud_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion");

            migrationBuilder.DropForeignKey(
                name: "FK_RelacionesInput_PasosSolicitud_PasoSolicitudId",
                table: "RelacionesInput");

            migrationBuilder.DropForeignKey(
                name: "FK_RelacionesUsuarioGrupo_GruposAprobacion_GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo");

            migrationBuilder.DropIndex(
                name: "IX_RelacionesUsuarioGrupo_GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo");

            migrationBuilder.DropIndex(
                name: "IX_RelacionesGrupoAprobacion_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion");

            migrationBuilder.DropColumn(
                name: "GrupoAprobacionIdGrupo",
                table: "RelacionesUsuarioGrupo");

            migrationBuilder.DropColumn(
                name: "FechaDecision",
                table: "DecisionesUsuario");

            migrationBuilder.AlterColumn<int>(
                name: "ReglaAprobacion",
                table: "PasosSolicitud",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CaminoId",
                table: "PasosSolicitud",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelacionesGrupoAprobacion_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion",
                column: "PasoSolicitudId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comentarios_PasosSolicitud_PasoSolicitudId",
                table: "Comentarios",
                column: "PasoSolicitudId",
                principalTable: "PasosSolicitud",
                principalColumn: "IdPasoSolicitud");

            migrationBuilder.AddForeignKey(
                name: "FK_PasosSolicitud_CaminosParalelos_CaminoId",
                table: "PasosSolicitud",
                column: "CaminoId",
                principalTable: "CaminosParalelos",
                principalColumn: "IdCamino",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RelacionesGrupoAprobacion_PasosSolicitud_PasoSolicitudId",
                table: "RelacionesGrupoAprobacion",
                column: "PasoSolicitudId",
                principalTable: "PasosSolicitud",
                principalColumn: "IdPasoSolicitud");

            migrationBuilder.AddForeignKey(
                name: "FK_RelacionesInput_PasosSolicitud_PasoSolicitudId",
                table: "RelacionesInput",
                column: "PasoSolicitudId",
                principalTable: "PasosSolicitud",
                principalColumn: "IdPasoSolicitud");
        }
    }
}
