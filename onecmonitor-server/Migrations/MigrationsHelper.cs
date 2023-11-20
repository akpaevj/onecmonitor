using Microsoft.EntityFrameworkCore.Migrations;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Migrations
{
    public class MigrationsHelper
    {
        public static void MigrateBuiltInData(MigrationBuilder migrationBuilder)
        {
            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.ServerMonitoringId,
                "Server monitoring",
                LogTemplate.ServerMonitoringTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.WaitingsOnManagedLocksId,
                "Waitings on managed locks",
                LogTemplate.WaitingsOnManagedLocksTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.TimeoutsOnManagedLocksId,
                "Timeouts on managed locks",
                LogTemplate.TimeoutsOnManagedLocksTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.DeadlocksOnManagedLocksId,
                "Deadlocks on managed locks",
                LogTemplate.DeadlocksOnManagedLocksTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.CallScallsId,
                "Calls and scalls",
                LogTemplate.CallsScallTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.VrsId,
                "VRS responses and requestes",
                LogTemplate.VrsTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.FullId,
                "Full",
                LogTemplate.FullTemplate);
        }

        public static void AddBuiltInLogTemplate(MigrationBuilder migrationBuilder, Guid id, string name, string content)
        {
            migrationBuilder.Sql(
                $"""
                INSERT INTO LogTemplates 
                VALUES (
                    '{id}', 
                    '{name}',
                    '{content}'
                    )
                """);
        }
    }
}
