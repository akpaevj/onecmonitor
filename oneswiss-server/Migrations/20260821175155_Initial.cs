using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "AccessGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Agents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsToken = table.Column<bool>(type: "boolean", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    User = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    DefaultForClusters = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultV8Admin = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultConfigRepositoriesAdmin = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrServerProxySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrServerProxySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dbms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Host = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dbms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLoggingServiceSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReportsTtl = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLoggingServiceSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Report = table.Column<string>(type: "text", nullable: false),
                    Screenshot = table.Column<byte[]>(type: "bytea", nullable: false),
                    Hash = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    DataPath = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFaulted = table.Column<bool>(type: "boolean", nullable: false),
                    FinishDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    CommonDestination = table.Column<bool>(type: "boolean", nullable: false),
                    StartWhenDiscoverNewConfigVersion = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    SendTo = table.Column<string>(type: "text", nullable: false),
                    NotificationTypes = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    Recipient = table.Column<string>(type: "text", nullable: false),
                    Additionalinfo = table.Column<string>(type: "text", nullable: false),
                    CustomNotificationsKeys = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechLogFilters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Filter = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechLogFilters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechLogSeances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StartMode = table.Column<int>(type: "integer", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    DirectSending = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechLogSeances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelegramBotSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBotSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsersGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsersGroups_UsersGroups_ParentId",
                        column: x => x.ParentId,
                        principalTable: "UsersGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AccessGroupApplicationRole",
                columns: table => new
                {
                    AccessGroupsId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroupApplicationRole", x => new { x.AccessGroupsId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_AccessGroupApplicationRole_AccessGroups_AccessGroupsId",
                        column: x => x.AccessGroupsId,
                        principalTable: "AccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessGroupApplicationRole_AspNetRoles_RolesId",
                        column: x => x.RolesId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clusters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClusterInternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Host = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    RagentPort = table.Column<int>(type: "integer", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialsId = table.Column<Guid>(type: "uuid", nullable: true)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Host = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    AgentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialsId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastReadVersion = table.Column<int>(type: "integer", nullable: false)
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
                name: "GitRepositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    TokenId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "EventLogSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DbmsId = table.Column<Guid>(type: "uuid", nullable: true),
                    DatabaseName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Table = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CredentialsId = table.Column<Guid>(type: "uuid", nullable: true),
                    InfoBaseNameRegex = table.Column<string>(type: "text", nullable: false),
                    DefaultTtl = table.Column<int>(type: "integer", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    DbmsId = table.Column<Guid>(type: "uuid", nullable: true),
                    DatabaseName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Table = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CredentialsId = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "CrServerProxyMiddlewares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DebugMode = table.Column<bool>(type: "boolean", nullable: false),
                    ExecutablePath = table.Column<string>(type: "text", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConnectAll = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrServerProxyMiddlewares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrServerProxyMiddlewares_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentMaintenanceTask",
                columns: table => new
                {
                    AgentsId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceTasksId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    NotificationRecipientId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomNotifications_NotificationRecipients_NotificationReci~",
                        column: x => x.NotificationRecipientId,
                        principalTable: "NotificationRecipients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AgentTechLogSeance",
                columns: table => new
                {
                    AgentsId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechLogSeancesId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    SeancesId = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplatesId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "AccessGroupUsersGroup",
                columns: table => new
                {
                    AccessGroupsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsersGroupsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGroupUsersGroup", x => new { x.AccessGroupsId, x.UsersGroupsId });
                    table.ForeignKey(
                        name: "FK_AccessGroupUsersGroup_AccessGroups_AccessGroupsId",
                        column: x => x.AccessGroupsId,
                        principalTable: "AccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AccessGroupUsersGroup_UsersGroups_UsersGroupsId",
                        column: x => x.UsersGroupsId,
                        principalTable: "UsersGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalName = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_UsersGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "UsersGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InfoBases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InfoBaseInternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    InfoBaseName = table.Column<string>(type: "text", nullable: false),
                    PublishAddress = table.Column<string>(type: "text", nullable: false),
                    CredentialsId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClusterId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InternalId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    GitUser = table.Column<string>(type: "text", nullable: true),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false)
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
                name: "ConfigurationRepositoryMiddlewareArgument",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CrServerProxyMiddlewareId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationRepositoryMiddlewareArgument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfigurationRepositoryMiddlewareArgument_CrServerProxyMidd~",
                        column: x => x.CrServerProxyMiddlewareId,
                        principalTable: "CrServerProxyMiddlewares",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CrServerProxyLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigurationRepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    CrServerProxyMiddlewareId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrServerProxyLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrServerProxyLocations_ConfigRepositories_ConfigurationRepo~",
                        column: x => x.ConfigurationRepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CrServerProxyLocations_CrServerProxyMiddlewares_CrServerPro~",
                        column: x => x.CrServerProxyMiddlewareId,
                        principalTable: "CrServerProxyMiddlewares",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventLogExportItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    InfoBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ttl = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogExportItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventLogExportItems_InfoBases_InfoBaseId",
                        column: x => x.InfoBaseId,
                        principalTable: "InfoBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InfoBaseMaintenanceTask",
                columns: table => new
                {
                    InfoBasesId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceTasksId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    NodeKind = table.Column<int>(type: "integer", nullable: false),
                    PreviousStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeftStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    RightStepId = table.Column<Guid>(type: "uuid", nullable: true),
                    PositionX = table.Column<double>(type: "double precision", nullable: false),
                    PositionY = table.Column<double>(type: "double precision", nullable: false),
                    CopyInfoBaseStep_SourceCredentialsId = table.Column<Guid>(type: "uuid", nullable: true),
                    CopyInfoBaseStep_SourceInfoBaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    CopyInfoBaseStep_DestinationCredentialsId = table.Column<Guid>(type: "uuid", nullable: true),
                    CopyInfoBaseStep_DestinationInfoBaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExecuteOneScriptStep_DebugMode = table.Column<bool>(type: "boolean", nullable: true),
                    ExecuteOneScriptStep_ExecutablePath = table.Column<string>(type: "text", nullable: true),
                    ExecuteOneScriptStep_FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartExternalDataProcessorStep_FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdateConfigurationStep_FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoadExtensionStep_FromConfigRepository = table.Column<bool>(type: "boolean", nullable: true),
                    LoadExtensionStep_LoadExactVersion = table.Column<bool>(type: "boolean", nullable: true),
                    LoadExtensionStep_Version = table.Column<int>(type: "integer", nullable: true),
                    LoadExtensionStep_ExtensionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LoadExtensionStep_FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoadExtensionStep_BaseConfigurationRepositoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoadExtensionStep_ConfigurationRepositoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeleteExtensionStep_ExtensionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LoadConfigurationStep_FromConfigRepository = table.Column<bool>(type: "boolean", nullable: true),
                    LoadConfigurationStep_LoadExactVersion = table.Column<bool>(type: "boolean", nullable: true),
                    LoadConfigurationStep_Version = table.Column<int>(type: "integer", nullable: true),
                    LoadConfigurationStep_FileId = table.Column<Guid>(type: "uuid", nullable: true),
                    LoadConfigurationStep_ConfigurationRepositoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    LockConnectionsStep_AccessCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LockConnectionsStep_Message = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_ConfigRepositories_LoadConfigurationStep_C~",
                        column: x => x.LoadConfigurationStep_ConfigurationRepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_ConfigRepositories_LoadExtensionStep_BaseC~",
                        column: x => x.LoadExtensionStep_BaseConfigurationRepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_ConfigRepositories_LoadExtensionStep_Confi~",
                        column: x => x.LoadExtensionStep_ConfigurationRepositoryId,
                        principalTable: "ConfigRepositories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_Credentials_CopyInfoBaseStep_DestinationCr~",
                        column: x => x.CopyInfoBaseStep_DestinationCredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_Credentials_CopyInfoBaseStep_SourceCredent~",
                        column: x => x.CopyInfoBaseStep_SourceCredentialsId,
                        principalTable: "Credentials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_Files_ExecuteOneScriptStep_FileId",
                        column: x => x.ExecuteOneScriptStep_FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_Files_LoadConfigurationStep_FileId",
                        column: x => x.LoadConfigurationStep_FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_Files_LoadExtensionStep_FileId",
                        column: x => x.LoadExtensionStep_FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_Files_StartExternalDataProcessorStep_FileId",
                        column: x => x.StartExternalDataProcessorStep_FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_Files_UpdateConfigurationStep_FileId",
                        column: x => x.UpdateConfigurationStep_FileId,
                        principalTable: "Files",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_InfoBases_CopyInfoBaseStep_DestinationInfo~",
                        column: x => x.CopyInfoBaseStep_DestinationInfoBaseId,
                        principalTable: "InfoBases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_InfoBases_CopyInfoBaseStep_SourceInfoBaseId",
                        column: x => x.CopyInfoBaseStep_SourceInfoBaseId,
                        principalTable: "InfoBases",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MaintenanceSteps_MaintenanceTasks_MaintenanceTaskId",
                        column: x => x.MaintenanceTaskId,
                        principalTable: "MaintenanceTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTaskLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsError = table.Column<bool>(type: "boolean", nullable: false),
                    IsFinish = table.Column<bool>(type: "boolean", nullable: false),
                    Message = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InfoBaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_AccessGroupApplicationRole_RolesId",
                table: "AccessGroupApplicationRole",
                column: "RolesId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGroupUsersGroup_UsersGroupsId",
                table: "AccessGroupUsersGroup",
                column: "UsersGroupsId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentMaintenanceTask_MaintenanceTasksId",
                table: "AgentMaintenanceTask",
                column: "MaintenanceTasksId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentTechLogSeance_TechLogSeancesId",
                table: "AgentTechLogSeance",
                column: "TechLogSeancesId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GroupId",
                table: "AspNetUsers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

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
                name: "IX_ConfigurationRepositoryMiddlewareArgument_CrServerProxyMidd~",
                table: "ConfigurationRepositoryMiddlewareArgument",
                column: "CrServerProxyMiddlewareId");

            migrationBuilder.CreateIndex(
                name: "IX_CrServerProxyLocations_ConfigurationRepositoryId",
                table: "CrServerProxyLocations",
                column: "ConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CrServerProxyLocations_CrServerProxyMiddlewareId",
                table: "CrServerProxyLocations",
                column: "CrServerProxyMiddlewareId");

            migrationBuilder.CreateIndex(
                name: "IX_CrServerProxyMiddlewares_FileId",
                table: "CrServerProxyMiddlewares",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomNotifications_NotificationRecipientId",
                table: "CustomNotifications",
                column: "NotificationRecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogExportItems_InfoBaseId",
                table: "EventLogExportItems",
                column: "InfoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogSettings_CredentialsId",
                table: "EventLogSettings",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_EventLogSettings_DbmsId",
                table: "EventLogSettings",
                column: "DbmsId");

            migrationBuilder.CreateIndex(
                name: "IX_GitRepositories_TokenId",
                table: "GitRepositories",
                column: "TokenId");

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
                name: "IX_LogTemplateTechLogSeance_TemplatesId",
                table: "LogTemplateTechLogSeance",
                column: "TemplatesId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_CopyInfoBaseStep_DestinationCredentialsId",
                table: "MaintenanceSteps",
                column: "CopyInfoBaseStep_DestinationCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_CopyInfoBaseStep_DestinationInfoBaseId",
                table: "MaintenanceSteps",
                column: "CopyInfoBaseStep_DestinationInfoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_CopyInfoBaseStep_SourceCredentialsId",
                table: "MaintenanceSteps",
                column: "CopyInfoBaseStep_SourceCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_CopyInfoBaseStep_SourceInfoBaseId",
                table: "MaintenanceSteps",
                column: "CopyInfoBaseStep_SourceInfoBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_ExecuteOneScriptStep_FileId",
                table: "MaintenanceSteps",
                column: "ExecuteOneScriptStep_FileId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LoadConfigurationStep_ConfigurationReposit~",
                table: "MaintenanceSteps",
                column: "LoadConfigurationStep_ConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LoadConfigurationStep_FileId",
                table: "MaintenanceSteps",
                column: "LoadConfigurationStep_FileId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LoadExtensionStep_BaseConfigurationReposit~",
                table: "MaintenanceSteps",
                column: "LoadExtensionStep_BaseConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LoadExtensionStep_ConfigurationRepositoryId",
                table: "MaintenanceSteps",
                column: "LoadExtensionStep_ConfigurationRepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_LoadExtensionStep_FileId",
                table: "MaintenanceSteps",
                column: "LoadExtensionStep_FileId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_MaintenanceTaskId",
                table: "MaintenanceSteps",
                column: "MaintenanceTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_StartExternalDataProcessorStep_FileId",
                table: "MaintenanceSteps",
                column: "StartExternalDataProcessorStep_FileId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSteps_UpdateConfigurationStep_FileId",
                table: "MaintenanceSteps",
                column: "UpdateConfigurationStep_FileId");

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
                name: "IX_TechLogSettings_CredentialsId",
                table: "TechLogSettings",
                column: "CredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_TechLogSettings_DbmsId",
                table: "TechLogSettings",
                column: "DbmsId");

            migrationBuilder.CreateIndex(
                name: "IX_UsersGroups_ParentId",
                table: "UsersGroups",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessGroupApplicationRole");

            migrationBuilder.DropTable(
                name: "AccessGroupUsersGroup");

            migrationBuilder.DropTable(
                name: "AgentMaintenanceTask");

            migrationBuilder.DropTable(
                name: "AgentTechLogSeance");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ConfigRepositoryUsers");

            migrationBuilder.DropTable(
                name: "ConfigurationRepositoryMiddlewareArgument");

            migrationBuilder.DropTable(
                name: "CrServerProxyLocations");

            migrationBuilder.DropTable(
                name: "CrServerProxySettings");

            migrationBuilder.DropTable(
                name: "CustomNotifications");

            migrationBuilder.DropTable(
                name: "ErrorLoggingServiceSettings");

            migrationBuilder.DropTable(
                name: "ErrorReports");

            migrationBuilder.DropTable(
                name: "EventLogExportItems");

            migrationBuilder.DropTable(
                name: "EventLogSettings");

            migrationBuilder.DropTable(
                name: "GitRepositories");

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
                name: "AccessGroups");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CrServerProxyMiddlewares");

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
                name: "UsersGroups");

            migrationBuilder.DropTable(
                name: "ConfigRepositories");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "InfoBases");

            migrationBuilder.DropTable(
                name: "MaintenanceTasks");

            migrationBuilder.DropTable(
                name: "Clusters");

            migrationBuilder.DropTable(
                name: "Agents");

            migrationBuilder.DropTable(
                name: "Credentials");
        }
    }
}
