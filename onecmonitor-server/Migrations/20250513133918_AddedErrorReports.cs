using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnecMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedErrorReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Date",
                table: "ErrorReports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "ErrorReports");
        }
    }
}
