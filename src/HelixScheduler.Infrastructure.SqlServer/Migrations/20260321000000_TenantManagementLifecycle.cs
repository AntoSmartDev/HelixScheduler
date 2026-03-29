using HelixScheduler.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelixScheduler.Infrastructure.Migrations
{
    [DbContext(typeof(SchedulerDbContext))]
    [Migration("20260321000000_TenantManagementLifecycle")]
    public partial class TenantManagementLifecycle : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tenants");
        }
    }
}
