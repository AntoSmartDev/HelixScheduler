using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelixScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20260321050000_BusyEventIntegrationHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalKey",
                table: "BusyEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusyEvents_TenantId_ExternalKey",
                table: "BusyEvents",
                columns: new[] { "TenantId", "ExternalKey" },
                unique: true,
                filter: "[ExternalKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusyEvents_TenantId_ExternalKey",
                table: "BusyEvents");

            migrationBuilder.DropColumn(
                name: "ExternalKey",
                table: "BusyEvents");
        }
    }
}
