using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FluentisCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCargo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<int>(
                name: "CargoId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cargo",
                columns: table => new
                {
                    IdCargo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    JefeCargoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargo", x => x.IdCargo);
                    table.ForeignKey(
                        name: "FK_Cargo_Roles_JefeCargoId",
                        column: x => x.JefeCargoId,
                        principalTable: "Roles",
                        principalColumn: "IdRol");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cargo_JefeCargoId",
                table: "Cargo",
                column: "JefeCargoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Cargo_RolId",
                table: "Usuarios",
                column: "RolId",
                principalTable: "Cargo",
                principalColumn: "IdCargo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Cargo_RolId",
                table: "Usuarios");

            migrationBuilder.DropTable(
                name: "Cargo");

            migrationBuilder.DropColumn(
                name: "CargoId",
                table: "Usuarios");

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
    }
}
