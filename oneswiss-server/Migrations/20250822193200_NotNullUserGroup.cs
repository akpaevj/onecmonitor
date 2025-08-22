using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSwiss.Server.Migrations
{
    /// <inheritdoc />
    public partial class NotNullUserGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UsersGroups_GroupId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "GroupId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UsersGroups_GroupId",
                table: "AspNetUsers",
                column: "GroupId",
                principalTable: "UsersGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UsersGroups_GroupId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "GroupId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UsersGroups_GroupId",
                table: "AspNetUsers",
                column: "GroupId",
                principalTable: "UsersGroups",
                principalColumn: "Id");
        }
    }
}
