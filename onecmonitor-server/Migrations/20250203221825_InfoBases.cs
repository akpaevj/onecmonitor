using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnecMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class InfoBases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InfoBases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PublishAddress = table.Column<string>(type: "TEXT", nullable: false),
                    AdminUser = table.Column<string>(type: "TEXT", nullable: false),
                    AdminPassword = table.Column<string>(type: "TEXT", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoBases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfoBases_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InfoBases_AgentId",
                table: "InfoBases",
                column: "AgentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InfoBases");
        }
    }
}
