using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnecMonitor.Server.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InstanceName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    User = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    DefaultForClusters = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultV8Admin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechLogFilters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Filter = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechLogFilters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechLogSeances",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    StartMode = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDateTime = table.Column<long>(type: "INTEGER", nullable: false),
                    Duration = table.Column<int>(type: "INTEGER", nullable: false),
                    DirectSending = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechLogSeances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechLogSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClickHouseHost = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClickHousePort = table.Column<int>(type: "INTEGER", nullable: false),
                    ClickHouseDatabase = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClickHouseUser = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClickHousePassword = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechLogSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UpdateInfoBaseTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    StartDateTime = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateInfoBaseTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "V8Files",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    DataPath = table.Column<string>(type: "TEXT", nullable: false),
                    IsUpdate = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExtension = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsConfiguration = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsExternalDataProcessor = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_V8Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clusters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ClusterInternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Host = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialsId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clusters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clusters_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Clusters_Credentials_CredentialsId",
                        column: x => x.CredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentTechLogSeance",
                columns: table => new
                {
                    AgentsId = table.Column<string>(type: "TEXT", nullable: false),
                    SeancesId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTechLogSeance", x => new { x.AgentsId, x.SeancesId });
                    table.ForeignKey(
                        name: "FK_AgentTechLogSeance_Agents_AgentsId",
                        column: x => x.AgentsId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentTechLogSeance_TechLogSeances_SeancesId",
                        column: x => x.SeancesId,
                        principalTable: "TechLogSeances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LogTemplateTechLogSeance",
                columns: table => new
                {
                    SeancesId = table.Column<string>(type: "TEXT", nullable: false),
                    TemplatesId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogTemplateTechLogSeance", x => new { x.SeancesId, x.TemplatesId });
                    table.ForeignKey(
                        name: "FK_LogTemplateTechLogSeance_LogTemplates_TemplatesId",
                        column: x => x.TemplatesId,
                        principalTable: "LogTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogTemplateTechLogSeance_TechLogSeances_SeancesId",
                        column: x => x.SeancesId,
                        principalTable: "TechLogSeances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    AccessCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_V8Files_FileId",
                        column: x => x.FileId,
                        principalTable: "V8Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UpdateInfoBaseTaskV8File",
                columns: table => new
                {
                    FilesId = table.Column<string>(type: "TEXT", nullable: false),
                    UpdateTasksId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateInfoBaseTaskV8File", x => new { x.FilesId, x.UpdateTasksId });
                    table.ForeignKey(
                        name: "FK_UpdateInfoBaseTaskV8File_UpdateInfoBaseTasks_UpdateTasksId",
                        column: x => x.UpdateTasksId,
                        principalTable: "UpdateInfoBaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UpdateInfoBaseTaskV8File_V8Files_FilesId",
                        column: x => x.FilesId,
                        principalTable: "V8Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceStepNodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    LeftNodeId = table.Column<string>(type: "TEXT", nullable: true),
                    RightNodeId = table.Column<string>(type: "TEXT", nullable: true),
                    StepId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceStepNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceStepNodes_MaintenanceStepNodes_LeftNodeId",
                        column: x => x.LeftNodeId,
                        principalTable: "MaintenanceStepNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceStepNodes_MaintenanceStepNodes_RightNodeId",
                        column: x => x.RightNodeId,
                        principalTable: "MaintenanceStepNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceStepNodes_MaintenanceSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "MaintenanceSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    StartDateTime = table.Column<long>(type: "INTEGER", nullable: false),
                    IsFaulted = table.Column<bool>(type: "INTEGER", nullable: false),
                    FinishDateTime = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    RootNodeId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceTasks_MaintenanceStepNodes_RootNodeId",
                        column: x => x.RootNodeId,
                        principalTable: "MaintenanceStepNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InfoBases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InfoBaseInternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    InfoBaseName = table.Column<string>(type: "TEXT", nullable: false),
                    PublishAddress = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialsId = table.Column<string>(type: "TEXT", nullable: false),
                    ClusterId = table.Column<string>(type: "TEXT", nullable: false),
                    MaintenanceTaskId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoBases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InfoBases_Clusters_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Clusters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InfoBases_Credentials_CredentialsId",
                        column: x => x.CredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InfoBases_MaintenanceTasks_MaintenanceTaskId",
                        column: x => x.MaintenanceTaskId,
                        principalTable: "MaintenanceTasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InfoBaseUpdateInfoBaseTask",
                columns: table => new
                {
                    InfoBasesId = table.Column<string>(type: "TEXT", nullable: false),
                    UpdateTasksId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoBaseUpdateInfoBaseTask", x => new { x.InfoBasesId, x.UpdateTasksId });
                    table.ForeignKey(
                        name: "FK_InfoBaseUpdateInfoBaseTask_InfoBases_InfoBasesId",
                        column: x => x.InfoBasesId,
                        principalTable: "InfoBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InfoBaseUpdateInfoBaseTask_UpdateInfoBaseTasks_UpdateTasksId",
                        column: x => x.UpdateTasksId,
                        principalTable: "UpdateInfoBaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceStepNodeLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<long>(type: "INTEGER", nullable: false),
                    IsError = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFinish = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    InfoBaseId = table.Column<string>(type: "TEXT", nullable: false),
                    StepNodeId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceStepNodeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceStepNodeLogs_InfoBases_InfoBaseId",
                        column: x => x.InfoBaseId,
                        principalTable: "InfoBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceStepNodeLogs_MaintenanceStepNodes_StepNodeId",
                        column: x => x.StepNodeId,
                        principalTable: "MaintenanceStepNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UpdateInfoBaseTaskLogItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<long>(type: "INTEGER", nullable: false),
                    IsError = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFinish = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    InfoBaseId = table.Column<string>(type: "TEXT", nullable: false),
                    TaskId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateInfoBaseTaskLogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpdateInfoBaseTaskLogItems_InfoBases_InfoBaseId",
                        column: x => x.InfoBaseId,
                        principalTable: "InfoBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UpdateInfoBaseTaskLogItems_UpdateInfoBaseTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "UpdateInfoBaseTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentTechLogSeance_SeancesId",
                table: "AgentTechLogSeance",
                column: "SeancesId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_AgentId",
                table: "Clusters",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_CredentialsId",
                table: "Clusters",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoBases_ClusterId",
                table: "InfoBases",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoBases_CredentialsId",
                table: "InfoBases",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoBases_MaintenanceTaskId",
                table: "InfoBases",
                column: "MaintenanceTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoBaseUpdateInfoBaseTask_UpdateTasksId",
                table: "InfoBaseUpdateInfoBaseTask",
                column: "UpdateTasksId");

            migrationBuilder.CreateIndex(
                name: "IX_LogTemplateTechLogSeance_TemplatesId",
                table: "LogTemplateTechLogSeance",
                column: "TemplatesId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceStepNodeLogs_InfoBaseId",
                table: "MaintenanceStepNodeLogs",
                column: "InfoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceStepNodeLogs_StepNodeId",
                table: "MaintenanceStepNodeLogs",
                column: "StepNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceStepNodes_LeftNodeId",
                table: "MaintenanceStepNodes",
                column: "LeftNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceStepNodes_RightNodeId",
                table: "MaintenanceStepNodes",
                column: "RightNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceStepNodes_StepId",
                table: "MaintenanceStepNodes",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_FileId",
                table: "MaintenanceSteps",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTasks_RootNodeId",
                table: "MaintenanceTasks",
                column: "RootNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateInfoBaseTaskLogItems_InfoBaseId",
                table: "UpdateInfoBaseTaskLogItems",
                column: "InfoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateInfoBaseTaskLogItems_TaskId",
                table: "UpdateInfoBaseTaskLogItems",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateInfoBaseTaskV8File_UpdateTasksId",
                table: "UpdateInfoBaseTaskV8File",
                column: "UpdateTasksId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentTechLogSeance");

            migrationBuilder.DropTable(
                name: "InfoBaseUpdateInfoBaseTask");

            migrationBuilder.DropTable(
                name: "LogTemplateTechLogSeance");

            migrationBuilder.DropTable(
                name: "MaintenanceStepNodeLogs");

            migrationBuilder.DropTable(
                name: "TechLogFilters");

            migrationBuilder.DropTable(
                name: "TechLogSettings");

            migrationBuilder.DropTable(
                name: "UpdateInfoBaseTaskLogItems");

            migrationBuilder.DropTable(
                name: "UpdateInfoBaseTaskV8File");

            migrationBuilder.DropTable(
                name: "LogTemplates");

            migrationBuilder.DropTable(
                name: "TechLogSeances");

            migrationBuilder.DropTable(
                name: "InfoBases");

            migrationBuilder.DropTable(
                name: "UpdateInfoBaseTasks");

            migrationBuilder.DropTable(
                name: "Clusters");

            migrationBuilder.DropTable(
                name: "MaintenanceTasks");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Credentials");

            migrationBuilder.DropTable(
                name: "MaintenanceStepNodes");

            migrationBuilder.DropTable(
                name: "MaintenanceSteps");

            migrationBuilder.DropTable(
                name: "V8Files");
        }
    }
}
