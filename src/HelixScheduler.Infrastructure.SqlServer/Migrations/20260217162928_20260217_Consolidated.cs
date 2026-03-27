using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelixScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20260217_Consolidated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusyEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusyEvents", x => x.Id);
                    table.UniqueConstraint("AK_BusyEvents_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_BusyEvents_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemoScenarioStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaseDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SeedVersion = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoScenarioStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoScenarioStates_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceProperties", x => x.Id);
                    table.UniqueConstraint("AK_ResourceProperties_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ResourceProperties_ResourceProperties_TenantId_ParentId",
                        columns: x => new { x.TenantId, x.ParentId },
                        principalTable: "ResourceProperties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceProperties_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTypes", x => x.Id);
                    table.UniqueConstraint("AK_ResourceTypes_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ResourceTypes_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<byte>(type: "tinyint", nullable: false),
                    IsExclude = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    FromDateUtc = table.Column<DateOnly>(type: "date", nullable: true),
                    ToDateUtc = table.Column<DateOnly>(type: "date", nullable: true),
                    SingleDateUtc = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    DaysOfWeekMask = table.Column<int>(type: "int", nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    IntervalDays = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                    table.UniqueConstraint("AK_Rules_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Rules_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsSchedulable = table.Column<bool>(type: "bit", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TypeId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.UniqueConstraint("AK_Resources_TenantId_Id", x => new { x.TenantId, x.Id });
                    table.CheckConstraint("CK_Resources_Capacity", "[Capacity] >= 1");
                    table.ForeignKey(
                        name: "FK_Resources_ResourceTypes_TenantId_TypeId",
                        columns: x => new { x.TenantId, x.TypeId },
                        principalTable: "ResourceTypes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resources_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTypeProperties",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceTypeId = table.Column<int>(type: "int", nullable: false),
                    PropertyDefinitionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTypeProperties", x => new { x.TenantId, x.ResourceTypeId, x.PropertyDefinitionId });
                    table.ForeignKey(
                        name: "FK_ResourceTypeProperties_ResourceProperties_TenantId_PropertyDefinitionId",
                        columns: x => new { x.TenantId, x.PropertyDefinitionId },
                        principalTable: "ResourceProperties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceTypeProperties_ResourceTypes_TenantId_ResourceTypeId",
                        columns: x => new { x.TenantId, x.ResourceTypeId },
                        principalTable: "ResourceTypes",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusyEventResources",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusyEventId = table.Column<long>(type: "bigint", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusyEventResources", x => new { x.TenantId, x.BusyEventId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_BusyEventResources_BusyEvents_TenantId_BusyEventId",
                        columns: x => new { x.TenantId, x.BusyEventId },
                        principalTable: "BusyEvents",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusyEventResources_Resources_TenantId_ResourceId",
                        columns: x => new { x.TenantId, x.ResourceId },
                        principalTable: "Resources",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourcePropertyLinks",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourcePropertyLinks", x => new { x.TenantId, x.ResourceId, x.PropertyId });
                    table.ForeignKey(
                        name: "FK_ResourcePropertyLinks_ResourceProperties_TenantId_PropertyId",
                        columns: x => new { x.TenantId, x.PropertyId },
                        principalTable: "ResourceProperties",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourcePropertyLinks_Resources_TenantId_ResourceId",
                        columns: x => new { x.TenantId, x.ResourceId },
                        principalTable: "Resources",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRelations",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentResourceId = table.Column<int>(type: "int", nullable: false),
                    ChildResourceId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRelations", x => new { x.TenantId, x.ParentResourceId, x.ChildResourceId, x.RelationType });
                    table.ForeignKey(
                        name: "FK_ResourceRelations_Resources_TenantId_ChildResourceId",
                        columns: x => new { x.TenantId, x.ChildResourceId },
                        principalTable: "Resources",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRelations_Resources_TenantId_ParentResourceId",
                        columns: x => new { x.TenantId, x.ParentResourceId },
                        principalTable: "Resources",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleResources",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<long>(type: "bigint", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleResources", x => new { x.TenantId, x.RuleId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_RuleResources_Resources_TenantId_ResourceId",
                        columns: x => new { x.TenantId, x.ResourceId },
                        principalTable: "Resources",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleResources_Rules_TenantId_RuleId",
                        columns: x => new { x.TenantId, x.RuleId },
                        principalTable: "Rules",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusyEventResources_TenantId_ResourceId_BusyEventId",
                table: "BusyEventResources",
                columns: new[] { "TenantId", "ResourceId", "BusyEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusyEvents_TenantId_StartUtc_EndUtc",
                table: "BusyEvents",
                columns: new[] { "TenantId", "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DemoScenarioStates_TenantId",
                table: "DemoScenarioStates",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProperties_TenantId_Key",
                table: "ResourceProperties",
                columns: new[] { "TenantId", "Key" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProperties_TenantId_ParentId",
                table: "ResourceProperties",
                columns: new[] { "TenantId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourcePropertyLinks_TenantId_PropertyId_ResourceId",
                table: "ResourcePropertyLinks",
                columns: new[] { "TenantId", "PropertyId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRelations_TenantId_ChildResourceId",
                table: "ResourceRelations",
                columns: new[] { "TenantId", "ChildResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Code",
                table: "Resources",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_Code",
                table: "Resources",
                columns: new[] { "TenantId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_IsSchedulable",
                table: "Resources",
                columns: new[] { "TenantId", "IsSchedulable" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_TypeId",
                table: "Resources",
                columns: new[] { "TenantId", "TypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypeProperties_TenantId_PropertyDefinitionId_ResourceTypeId",
                table: "ResourceTypeProperties",
                columns: new[] { "TenantId", "PropertyDefinitionId", "ResourceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_TenantId_Key",
                table: "ResourceTypes",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuleResources_TenantId_ResourceId_RuleId",
                table: "RuleResources",
                columns: new[] { "TenantId", "ResourceId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_TenantId_FromDateUtc_ToDateUtc_SingleDateUtc",
                table: "Rules",
                columns: new[] { "TenantId", "FromDateUtc", "ToDateUtc", "SingleDateUtc" })
                .Annotation("SqlServer:Include", new[] { "IsExclude" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Key",
                table: "Tenants",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusyEventResources");

            migrationBuilder.DropTable(
                name: "DemoScenarioStates");

            migrationBuilder.DropTable(
                name: "ResourcePropertyLinks");

            migrationBuilder.DropTable(
                name: "ResourceRelations");

            migrationBuilder.DropTable(
                name: "ResourceTypeProperties");

            migrationBuilder.DropTable(
                name: "RuleResources");

            migrationBuilder.DropTable(
                name: "BusyEvents");

            migrationBuilder.DropTable(
                name: "ResourceProperties");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Rules");

            migrationBuilder.DropTable(
                name: "ResourceTypes");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
