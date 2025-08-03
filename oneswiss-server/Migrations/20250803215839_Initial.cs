using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSwiss.Server.Migrations
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
                    DefaultV8Admin = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultConfigRepositoriesAdmin = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dbms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Host = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dbms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeleteExtensionSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ExtensionName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeleteExtensionSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLoggingServiceSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReportsTtl = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLoggingServiceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorReports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Report = table.Column<string>(type: "TEXT", nullable: false),
                    Screenshot = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Hash = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    DataPath = table.Column<string>(type: "TEXT", nullable: false),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LockConnectionsSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccessCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LockConnectionsSteps", x => x.Id);
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
                name: "MaintenanceTasks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartDateTime = table.Column<long>(type: "INTEGER", nullable: false),
                    IsFaulted = table.Column<bool>(type: "INTEGER", nullable: false),
                    FinishDateTime = table.Column<long>(type: "INTEGER", nullable: false),
                    IsTemplate = table.Column<bool>(type: "INTEGER", nullable: false),
                    CommonDestination = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    SendTo = table.Column<string>(type: "TEXT", nullable: false),
                    NotificationTypes = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    Recipient = table.Column<string>(type: "TEXT", nullable: false),
                    Additionalinfo = table.Column<string>(type: "TEXT", nullable: false),
                    CustomNotificationsKeys = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
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
                name: "TelegramBotSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBotSettings", x => x.Id);
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConfigRepositories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialsId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigRepositories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigRepositories_Agents_AgentId",
                        column: x => x.AgentId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConfigRepositories_Credentials_CredentialsId",
                        column: x => x.CredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventLogSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DbmsId = table.Column<string>(type: "TEXT", nullable: true),
                    DatabaseName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Table = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CredentialsId = table.Column<string>(type: "TEXT", nullable: true),
                    InfoBaseNameRegex = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventLogSettings_Credentials_CredentialsId",
                        column: x => x.CredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventLogSettings_Dbms_DbmsId",
                        column: x => x.DbmsId,
                        principalTable: "Dbms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechLogSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DbmsId = table.Column<string>(type: "TEXT", nullable: true),
                    DatabaseName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Table = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CredentialsId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechLogSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechLogSettings_Credentials_CredentialsId",
                        column: x => x.CredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechLogSettings_Dbms_DbmsId",
                        column: x => x.DbmsId,
                        principalTable: "Dbms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExecuteOneScriptSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DebugMode = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExecutablePath = table.Column<string>(type: "TEXT", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecuteOneScriptSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecuteOneScriptSteps_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StartExternalDataProcessorSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartExternalDataProcessorSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StartExternalDataProcessorSteps_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UpdateConfigurationSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateConfigurationSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpdateConfigurationSteps_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentMaintenanceTask",
                columns: table => new
                {
                    AgentsId = table.Column<string>(type: "TEXT", nullable: false),
                    MaintenanceTasksId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentMaintenanceTask", x => new { x.AgentsId, x.MaintenanceTasksId });
                    table.ForeignKey(
                        name: "FK_AgentMaintenanceTask_Agents_AgentsId",
                        column: x => x.AgentsId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentMaintenanceTask_MaintenanceTasks_MaintenanceTasksId",
                        column: x => x.MaintenanceTasksId,
                        principalTable: "MaintenanceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomNotifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    NotificationRecipientId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomNotifications_NotificationRecipients_NotificationRecipientId",
                        column: x => x.NotificationRecipientId,
                        principalTable: "NotificationRecipients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentTechLogSeance",
                columns: table => new
                {
                    AgentsId = table.Column<string>(type: "TEXT", nullable: false),
                    TechLogSeancesId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTechLogSeance", x => new { x.AgentsId, x.TechLogSeancesId });
                    table.ForeignKey(
                        name: "FK_AgentTechLogSeance_Agents_AgentsId",
                        column: x => x.AgentsId,
                        principalTable: "Agents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentTechLogSeance_TechLogSeances_TechLogSeancesId",
                        column: x => x.TechLogSeancesId,
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
                name: "InfoBases",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InfoBaseInternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    InfoBaseName = table.Column<string>(type: "TEXT", nullable: false),
                    PublishAddress = table.Column<string>(type: "TEXT", nullable: false),
                    CredentialsId = table.Column<string>(type: "TEXT", nullable: true),
                    ClusterId = table.Column<string>(type: "TEXT", nullable: false)
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
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConfigRepositoryUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    InternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    GitUser = table.Column<string>(type: "TEXT", nullable: true),
                    Deleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RepositoryId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigRepositoryUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigRepositoryUsers_ConfigRepositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoadConfigurationSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FromConfigRepository = table.Column<bool>(type: "INTEGER", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    ConfigurationRepositoryId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadConfigurationSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoadConfigurationSteps_ConfigRepositories_ConfigurationRepositoryId",
                        column: x => x.ConfigurationRepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LoadConfigurationSteps_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LoadExtensionSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FromConfigRepository = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExtensionName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: true),
                    ConfigurationRepositoryId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadExtensionSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoadExtensionSteps_ConfigRepositories_ConfigurationRepositoryId",
                        column: x => x.ConfigurationRepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LoadExtensionSteps_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CopyInfoBaseSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SourceCredentialsId = table.Column<string>(type: "TEXT", nullable: true),
                    SourceInfoBaseId = table.Column<string>(type: "TEXT", nullable: true),
                    DestinationCredentialsId = table.Column<string>(type: "TEXT", nullable: true),
                    DestinationInfoBaseId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopyInfoBaseSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CopyInfoBaseSteps_Credentials_DestinationCredentialsId",
                        column: x => x.DestinationCredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CopyInfoBaseSteps_Credentials_SourceCredentialsId",
                        column: x => x.SourceCredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CopyInfoBaseSteps_InfoBases_DestinationInfoBaseId",
                        column: x => x.DestinationInfoBaseId,
                        principalTable: "InfoBases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CopyInfoBaseSteps_InfoBases_SourceInfoBaseId",
                        column: x => x.SourceInfoBaseId,
                        principalTable: "InfoBases",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InfoBaseMaintenanceTask",
                columns: table => new
                {
                    InfoBasesId = table.Column<string>(type: "TEXT", nullable: false),
                    MaintenanceTasksId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoBaseMaintenanceTask", x => new { x.InfoBasesId, x.MaintenanceTasksId });
                    table.ForeignKey(
                        name: "FK_InfoBaseMaintenanceTask_InfoBases_InfoBasesId",
                        column: x => x.InfoBasesId,
                        principalTable: "InfoBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InfoBaseMaintenanceTask_MaintenanceTasks_MaintenanceTasksId",
                        column: x => x.MaintenanceTasksId,
                        principalTable: "MaintenanceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    StepId = table.Column<string>(type: "TEXT", nullable: false),
                    MaintenanceTaskId = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    NodeKind = table.Column<int>(type: "INTEGER", nullable: false),
                    PreviousStepId = table.Column<string>(type: "TEXT", nullable: true),
                    LeftStepId = table.Column<string>(type: "TEXT", nullable: true),
                    RightStepId = table.Column<string>(type: "TEXT", nullable: true),
                    LockConnectionsStepId = table.Column<string>(type: "TEXT", nullable: true),
                    LoadConfigurationStepId = table.Column<string>(type: "TEXT", nullable: true),
                    DeleteExtensionStepId = table.Column<string>(type: "TEXT", nullable: true),
                    LoadExtensionStepId = table.Column<string>(type: "TEXT", nullable: true),
                    UpdateConfigurationStepId = table.Column<string>(type: "TEXT", nullable: true),
                    StartExternalDataProcessorStepId = table.Column<string>(type: "TEXT", nullable: true),
                    ExecuteOneScriptStepId = table.Column<string>(type: "TEXT", nullable: true),
                    CopyInfoBaseStepId = table.Column<string>(type: "TEXT", nullable: true),
                    PositionX = table.Column<double>(type: "REAL", nullable: false),
                    PositionY = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_CopyInfoBaseSteps_CopyInfoBaseStepId",
                        column: x => x.CopyInfoBaseStepId,
                        principalTable: "CopyInfoBaseSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_DeleteExtensionSteps_DeleteExtensionStepId",
                        column: x => x.DeleteExtensionStepId,
                        principalTable: "DeleteExtensionSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_ExecuteOneScriptSteps_ExecuteOneScriptStepId",
                        column: x => x.ExecuteOneScriptStepId,
                        principalTable: "ExecuteOneScriptSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_LoadConfigurationSteps_LoadConfigurationStepId",
                        column: x => x.LoadConfigurationStepId,
                        principalTable: "LoadConfigurationSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_LoadExtensionSteps_LoadExtensionStepId",
                        column: x => x.LoadExtensionStepId,
                        principalTable: "LoadExtensionSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_LockConnectionsSteps_LockConnectionsStepId",
                        column: x => x.LockConnectionsStepId,
                        principalTable: "LockConnectionsSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_MaintenanceTasks_MaintenanceTaskId",
                        column: x => x.MaintenanceTaskId,
                        principalTable: "MaintenanceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_StartExternalDataProcessorSteps_StartExternalDataProcessorStepId",
                        column: x => x.StartExternalDataProcessorStepId,
                        principalTable: "StartExternalDataProcessorSteps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_UpdateConfigurationSteps_UpdateConfigurationStepId",
                        column: x => x.UpdateConfigurationStepId,
                        principalTable: "UpdateConfigurationSteps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTaskLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<long>(type: "INTEGER", nullable: false),
                    IsError = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsFinish = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    InfoBaseId = table.Column<string>(type: "TEXT", nullable: true),
                    StepId = table.Column<string>(type: "TEXT", nullable: true),
                    TaskId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTaskLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceTaskLogs_InfoBases_InfoBaseId",
                        column: x => x.InfoBaseId,
                        principalTable: "InfoBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceTaskLogs_MaintenanceSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "MaintenanceSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceTaskLogs_MaintenanceTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "MaintenanceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentMaintenanceTask_MaintenanceTasksId",
                table: "AgentMaintenanceTask",
                column: "MaintenanceTasksId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTechLogSeance_TechLogSeancesId",
                table: "AgentTechLogSeance",
                column: "TechLogSeancesId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_AgentId",
                table: "Clusters",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Clusters_CredentialsId",
                table: "Clusters",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigRepositories_AgentId",
                table: "ConfigRepositories",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigRepositories_CredentialsId",
                table: "ConfigRepositories",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigRepositoryUsers_RepositoryId",
                table: "ConfigRepositoryUsers",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CopyInfoBaseSteps_DestinationCredentialsId",
                table: "CopyInfoBaseSteps",
                column: "DestinationCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_CopyInfoBaseSteps_DestinationInfoBaseId",
                table: "CopyInfoBaseSteps",
                column: "DestinationInfoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CopyInfoBaseSteps_SourceCredentialsId",
                table: "CopyInfoBaseSteps",
                column: "SourceCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_CopyInfoBaseSteps_SourceInfoBaseId",
                table: "CopyInfoBaseSteps",
                column: "SourceInfoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomNotifications_NotificationRecipientId",
                table: "CustomNotifications",
                column: "NotificationRecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogSettings_CredentialsId",
                table: "EventLogSettings",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogSettings_DbmsId",
                table: "EventLogSettings",
                column: "DbmsId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecuteOneScriptSteps_FileId",
                table: "ExecuteOneScriptSteps",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoBaseMaintenanceTask_MaintenanceTasksId",
                table: "InfoBaseMaintenanceTask",
                column: "MaintenanceTasksId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoBases_ClusterId",
                table: "InfoBases",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_InfoBases_CredentialsId",
                table: "InfoBases",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_LoadConfigurationSteps_ConfigurationRepositoryId",
                table: "LoadConfigurationSteps",
                column: "ConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LoadConfigurationSteps_FileId",
                table: "LoadConfigurationSteps",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_LoadExtensionSteps_ConfigurationRepositoryId",
                table: "LoadExtensionSteps",
                column: "ConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LoadExtensionSteps_FileId",
                table: "LoadExtensionSteps",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_LogTemplateTechLogSeance_TemplatesId",
                table: "LogTemplateTechLogSeance",
                column: "TemplatesId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_CopyInfoBaseStepId",
                table: "MaintenanceSteps",
                column: "CopyInfoBaseStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_DeleteExtensionStepId",
                table: "MaintenanceSteps",
                column: "DeleteExtensionStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_ExecuteOneScriptStepId",
                table: "MaintenanceSteps",
                column: "ExecuteOneScriptStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LoadConfigurationStepId",
                table: "MaintenanceSteps",
                column: "LoadConfigurationStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LoadExtensionStepId",
                table: "MaintenanceSteps",
                column: "LoadExtensionStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LockConnectionsStepId",
                table: "MaintenanceSteps",
                column: "LockConnectionsStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_MaintenanceTaskId",
                table: "MaintenanceSteps",
                column: "MaintenanceTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_StartExternalDataProcessorStepId",
                table: "MaintenanceSteps",
                column: "StartExternalDataProcessorStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_UpdateConfigurationStepId",
                table: "MaintenanceSteps",
                column: "UpdateConfigurationStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTaskLogs_InfoBaseId",
                table: "MaintenanceTaskLogs",
                column: "InfoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTaskLogs_StepId",
                table: "MaintenanceTaskLogs",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTaskLogs_TaskId",
                table: "MaintenanceTaskLogs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_StartExternalDataProcessorSteps_FileId",
                table: "StartExternalDataProcessorSteps",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_TechLogSettings_CredentialsId",
                table: "TechLogSettings",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_TechLogSettings_DbmsId",
                table: "TechLogSettings",
                column: "DbmsId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateConfigurationSteps_FileId",
                table: "UpdateConfigurationSteps",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentMaintenanceTask");

            migrationBuilder.DropTable(
                name: "AgentTechLogSeance");

            migrationBuilder.DropTable(
                name: "ConfigRepositoryUsers");

            migrationBuilder.DropTable(
                name: "CustomNotifications");

            migrationBuilder.DropTable(
                name: "ErrorLoggingServiceSettings");

            migrationBuilder.DropTable(
                name: "ErrorReports");

            migrationBuilder.DropTable(
                name: "EventLogSettings");

            migrationBuilder.DropTable(
                name: "InfoBaseMaintenanceTask");

            migrationBuilder.DropTable(
                name: "LogTemplateTechLogSeance");

            migrationBuilder.DropTable(
                name: "MaintenanceTaskLogs");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "TechLogFilters");

            migrationBuilder.DropTable(
                name: "TechLogSettings");

            migrationBuilder.DropTable(
                name: "TelegramBotSettings");

            migrationBuilder.DropTable(
                name: "NotificationRecipients");

            migrationBuilder.DropTable(
                name: "LogTemplates");

            migrationBuilder.DropTable(
                name: "TechLogSeances");

            migrationBuilder.DropTable(
                name: "MaintenanceSteps");

            migrationBuilder.DropTable(
                name: "Dbms");

            migrationBuilder.DropTable(
                name: "CopyInfoBaseSteps");

            migrationBuilder.DropTable(
                name: "DeleteExtensionSteps");

            migrationBuilder.DropTable(
                name: "ExecuteOneScriptSteps");

            migrationBuilder.DropTable(
                name: "LoadConfigurationSteps");

            migrationBuilder.DropTable(
                name: "LoadExtensionSteps");

            migrationBuilder.DropTable(
                name: "LockConnectionsSteps");

            migrationBuilder.DropTable(
                name: "MaintenanceTasks");

            migrationBuilder.DropTable(
                name: "StartExternalDataProcessorSteps");

            migrationBuilder.DropTable(
                name: "UpdateConfigurationSteps");

            migrationBuilder.DropTable(
                name: "InfoBases");

            migrationBuilder.DropTable(
                name: "ConfigRepositories");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Clusters");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Credentials");
        }
    }
}
