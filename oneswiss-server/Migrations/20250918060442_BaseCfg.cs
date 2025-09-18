using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSwiss.Server.Migrations
{
    /// <inheritdoc />
    public partial class BaseCfg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LoadExtensionStep_BaseConfigurationRepositoryId",
                table: "MaintenanceSteps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BaseConfigurationRepositoryId",
                table: "GitSyncTaskItem",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LoadExtensionStep_BaseConfigurationRepositoryId",
                table: "MaintenanceSteps",
                column: "LoadExtensionStep_BaseConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_GitSyncTaskItem_BaseConfigurationRepositoryId",
                table: "GitSyncTaskItem",
                column: "BaseConfigurationRepositoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_GitSyncTaskItem_ConfigRepositories_BaseConfigurationRepositoryId",
                table: "GitSyncTaskItem",
                column: "BaseConfigurationRepositoryId",
                principalTable: "ConfigRepositories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintenanceSteps_ConfigRepositories_LoadExtensionStep_BaseConfigurationRepositoryId",
                table: "MaintenanceSteps",
                column: "LoadExtensionStep_BaseConfigurationRepositoryId",
                principalTable: "ConfigRepositories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GitSyncTaskItem_ConfigRepositories_BaseConfigurationRepositoryId",
                table: "GitSyncTaskItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintenanceSteps_ConfigRepositories_LoadExtensionStep_BaseConfigurationRepositoryId",
                table: "MaintenanceSteps");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceSteps_LoadExtensionStep_BaseConfigurationRepositoryId",
                table: "MaintenanceSteps");

            migrationBuilder.DropIndex(
                name: "IX_GitSyncTaskItem_BaseConfigurationRepositoryId",
                table: "GitSyncTaskItem");

            migrationBuilder.DropColumn(
                name: "LoadExtensionStep_BaseConfigurationRepositoryId",
                table: "MaintenanceSteps");

            migrationBuilder.DropColumn(
                name: "BaseConfigurationRepositoryId",
                table: "GitSyncTaskItem");
        }
    }
}
