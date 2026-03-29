using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelixScheduler.Infrastructure.Migrations
{
    [DbContext(typeof(SchedulerDbContext))]
    [Migration("20260321010000_ResourceCatalogManagementLifecycle")]
    public partial class ResourceCatalogManagementLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ResourceTypes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ResourceTypes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Resources");
        }
    }
}
