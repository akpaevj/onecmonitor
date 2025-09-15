using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSwiss.Server.Migrations
{
    /// <inheritdoc />
    public partial class ConfigReposVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StartWhenDiscoverNewConfigVersion",
                table: "MaintenanceTasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LoadConfigurationStep_LoadExactVersion",
                table: "MaintenanceSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoadConfigurationStep_Version",
                table: "MaintenanceSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LoadExtensionStep_LoadExactVersion",
                table: "MaintenanceSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoadExtensionStep_Version",
                table: "MaintenanceSteps",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastReadVersion",
                table: "ConfigRepositories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartWhenDiscoverNewConfigVersion",
                table: "MaintenanceTasks");

            migrationBuilder.DropColumn(
                name: "LoadConfigurationStep_LoadExactVersion",
                table: "MaintenanceSteps");

            migrationBuilder.DropColumn(
                name: "LoadConfigurationStep_Version",
                table: "MaintenanceSteps");

            migrationBuilder.DropColumn(
                name: "LoadExtensionStep_LoadExactVersion",
                table: "MaintenanceSteps");

            migrationBuilder.DropColumn(
                name: "LoadExtensionStep_Version",
                table: "MaintenanceSteps");

            migrationBuilder.DropColumn(
                name: "LastReadVersion",
                table: "ConfigRepositories");
        }
    }
}
