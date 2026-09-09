using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSwiss.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLogReduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReductionEnabled",
                table: "EventLogSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReductionHourUtc",
                table: "EventLogSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReductionSafetyMarginHours",
                table: "EventLogSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReducedUpTo",
                table: "EventLogExportItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReduceKeepDays",
                table: "EventLogExportItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ReduceSourceLog",
                table: "EventLogExportItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReductionEnabled",
                table: "EventLogSettings");

            migrationBuilder.DropColumn(
                name: "ReductionHourUtc",
                table: "EventLogSettings");

            migrationBuilder.DropColumn(
                name: "ReductionSafetyMarginHours",
                table: "EventLogSettings");

            migrationBuilder.DropColumn(
                name: "LastReducedUpTo",
                table: "EventLogExportItems");

            migrationBuilder.DropColumn(
                name: "ReduceKeepDays",
                table: "EventLogExportItems");

            migrationBuilder.DropColumn(
                name: "ReduceSourceLog",
                table: "EventLogExportItems");
        }
    }
}
