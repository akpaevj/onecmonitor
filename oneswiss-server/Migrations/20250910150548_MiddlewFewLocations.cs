using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSwiss.Server.Migrations
{
    /// <inheritdoc />
    public partial class MiddlewFewLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrServerProxyMiddlewares_CrServerProxyLocations_LocationId",
                table: "CrServerProxyMiddlewares");

            migrationBuilder.DropIndex(
                name: "IX_CrServerProxyMiddlewares_LocationId",
                table: "CrServerProxyMiddlewares");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "CrServerProxyMiddlewares");

            migrationBuilder.AddColumn<string>(
                name: "CrServerProxyMiddlewareId",
                table: "CrServerProxyLocations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrServerProxyLocations_CrServerProxyMiddlewareId",
                table: "CrServerProxyLocations",
                column: "CrServerProxyMiddlewareId");

            migrationBuilder.AddForeignKey(
                name: "FK_CrServerProxyLocations_CrServerProxyMiddlewares_CrServerProxyMiddlewareId",
                table: "CrServerProxyLocations",
                column: "CrServerProxyMiddlewareId",
                principalTable: "CrServerProxyMiddlewares",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrServerProxyLocations_CrServerProxyMiddlewares_CrServerProxyMiddlewareId",
                table: "CrServerProxyLocations");

            migrationBuilder.DropIndex(
                name: "IX_CrServerProxyLocations_CrServerProxyMiddlewareId",
                table: "CrServerProxyLocations");

            migrationBuilder.DropColumn(
                name: "CrServerProxyMiddlewareId",
                table: "CrServerProxyLocations");

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "CrServerProxyMiddlewares",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CrServerProxyMiddlewares_LocationId",
                table: "CrServerProxyMiddlewares",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_CrServerProxyMiddlewares_CrServerProxyLocations_LocationId",
                table: "CrServerProxyMiddlewares",
                column: "LocationId",
                principalTable: "CrServerProxyLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
