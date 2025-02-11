using Microsoft.EntityFrameworkCore.Migrations;
using OnecMonitor.Server.Models;

namespace OnecMonitor.Server.Migrations
{
    public static class MigrationsHelper
    {
        public static void MigrateBuiltInData(MigrationBuilder migrationBuilder)
        {
            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.ServerMonitoringId,
                "Мониторинг сервера",
                LogTemplate.ServerMonitoringTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.WaitingsOnManagedLocksId,
                "Ожидания на управляемых блокировках",
                LogTemplate.WaitingsOnManagedLocksTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.TimeoutsOnManagedLocksId,
                "Таймауты на управляемых блокировках",
                LogTemplate.TimeoutsOnManagedLocksTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.DeadlocksOnManagedLocksId,
                "Взаимоблокировки на управляемых блокировках",
                LogTemplate.DeadlocksOnManagedLocksTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.CallScallsId,
                "CALL и SCALL",
                LogTemplate.CallsScallTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.VrsId,
                "VRSREQUEST и VRSRESPONSE",
                LogTemplate.VrsTemplate);

            AppDbContext.AddBuiltInLogTemplate(
                migrationBuilder,
                LogTemplate.FullId,
                "Полный",
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
