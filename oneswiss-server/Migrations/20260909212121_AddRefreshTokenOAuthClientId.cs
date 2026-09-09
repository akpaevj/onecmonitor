using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSwiss.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenOAuthClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OAuthClientId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OAuthClientId",
                table: "RefreshTokens");
        }
    }
}
