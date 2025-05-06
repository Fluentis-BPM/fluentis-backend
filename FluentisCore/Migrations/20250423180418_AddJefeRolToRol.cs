using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class AddJefeRolToRol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JefeRolId",
                table: "Roles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_JefeRolId",
                table: "Roles",
                column: "JefeRolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Roles_JefeRolId",
                table: "Roles",
                column: "JefeRolId",
                principalTable: "Roles",
                principalColumn: "IdRol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Roles_JefeRolId",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_JefeRolId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "JefeRolId",
                table: "Roles");
        }
    }
}
