using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnecMonitor.Agent.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentInstance",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InstanceName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentInstance", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentInstance");
        }
    }
}
