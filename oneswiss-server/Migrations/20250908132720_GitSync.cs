using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSwiss.Server.Migrations
{
    /// <inheritdoc />
    public partial class GitSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsToken",
                table: "Credentials",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "Credentials",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CrServerProxyLocations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigurationRepositoryId = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrServerProxyLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrServerProxyLocations_ConfigRepositories_ConfigurationRepositoryId",
                        column: x => x.ConfigurationRepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CrServerProxySettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrServerProxySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GitRepositories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    TokenId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitRepositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitRepositories_Credentials_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Credentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GitSyncSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    BranchName = table.Column<string>(type: "TEXT", nullable: false),
                    LfsTrackers = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitSyncSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrServerProxyMiddlewares",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DebugMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExecutablePath = table.Column<string>(type: "TEXT", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    LocationId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectAll = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrServerProxyMiddlewares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrServerProxyMiddlewares_CrServerProxyLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "CrServerProxyLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrServerProxyMiddlewares_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GitSyncTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    BranchName = table.Column<string>(type: "TEXT", nullable: false),
                    GitRepositoryId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitSyncTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitSyncTasks_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GitSyncTasks_GitRepositories_GitRepositoryId",
                        column: x => x.GitRepositoryId,
                        principalTable: "GitRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationRepositoryMiddlewareArgument",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    CrServerProxyMiddlewareId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationRepositoryMiddlewareArgument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationRepositoryMiddlewareArgument_CrServerProxyMiddlewares_CrServerProxyMiddlewareId",
                        column: x => x.CrServerProxyMiddlewareId,
                        principalTable: "CrServerProxyMiddlewares",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GitSyncTaskItem",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    GitSyncTaskId = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfigurationRepositoryId = table.Column<string>(type: "TEXT", nullable: false),
                    IsExtension = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExportFolder = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigurationRepositoryVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    LfsTrackers = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GitSyncTaskItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GitSyncTaskItem_ConfigRepositories_ConfigurationRepositoryId",
                        column: x => x.ConfigurationRepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GitSyncTaskItem_GitSyncTasks_GitSyncTaskId",
                        column: x => x.GitSyncTaskId,
                        principalTable: "GitSyncTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationRepositoryMiddlewareArgument_CrServerProxyMiddlewareId",
                table: "ConfigurationRepositoryMiddlewareArgument",
                column: "CrServerProxyMiddlewareId");

            migrationBuilder.CreateIndex(
                name: "IX_CrServerProxyLocations_ConfigurationRepositoryId",
                table: "CrServerProxyLocations",
                column: "ConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CrServerProxyMiddlewares_FileId",
                table: "CrServerProxyMiddlewares",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_CrServerProxyMiddlewares_LocationId",
                table: "CrServerProxyMiddlewares",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_GitRepositories_TokenId",
                table: "GitRepositories",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_GitSyncTaskItem_ConfigurationRepositoryId",
                table: "GitSyncTaskItem",
                column: "ConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_GitSyncTaskItem_GitSyncTaskId",
                table: "GitSyncTaskItem",
                column: "GitSyncTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_GitSyncTasks_AgentId",
                table: "GitSyncTasks",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_GitSyncTasks_GitRepositoryId",
                table: "GitSyncTasks",
                column: "GitRepositoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationRepositoryMiddlewareArgument");

            migrationBuilder.DropTable(
                name: "CrServerProxySettings");

            migrationBuilder.DropTable(
                name: "GitSyncSettings");

            migrationBuilder.DropTable(
                name: "GitSyncTaskItem");

            migrationBuilder.DropTable(
                name: "CrServerProxyMiddlewares");

            migrationBuilder.DropTable(
                name: "GitSyncTasks");

            migrationBuilder.DropTable(
                name: "CrServerProxyLocations");

            migrationBuilder.DropTable(
                name: "GitRepositories");

            migrationBuilder.DropColumn(
                name: "IsToken",
                table: "Credentials");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "Credentials");
        }
    }
}
