using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelixScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Baseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusyEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusyEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DemoScenarioStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaseDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SeedVersion = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoScenarioStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceProperties_ResourceProperties_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ResourceProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.CheckConstraint("CK_Resources_Capacity", "[Capacity] >= 1");
                    table.ForeignKey(
                        name: "FK_Resources_ResourceTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTypeProperties",
                columns: table => new
                {
                    ResourceTypeId = table.Column<int>(type: "int", nullable: false),
                    PropertyDefinitionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTypeProperties", x => new { x.ResourceTypeId, x.PropertyDefinitionId });
                    table.ForeignKey(
                        name: "FK_ResourceTypeProperties_ResourceProperties_PropertyDefinitionId",
                        column: x => x.PropertyDefinitionId,
                        principalTable: "ResourceProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceTypeProperties_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusyEventResources",
                columns: table => new
                {
                    BusyEventId = table.Column<long>(type: "bigint", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusyEventResources", x => new { x.BusyEventId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_BusyEventResources_BusyEvents_BusyEventId",
                        column: x => x.BusyEventId,
                        principalTable: "BusyEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusyEventResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourcePropertyLinks",
                columns: table => new
                {
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourcePropertyLinks", x => new { x.ResourceId, x.PropertyId });
                    table.ForeignKey(
                        name: "FK_ResourcePropertyLinks_ResourceProperties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "ResourceProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourcePropertyLinks_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRelations",
                columns: table => new
                {
                    ParentResourceId = table.Column<int>(type: "int", nullable: false),
                    ChildResourceId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRelations", x => new { x.ParentResourceId, x.ChildResourceId, x.RelationType });
                    table.ForeignKey(
                        name: "FK_ResourceRelations_Resources_ChildResourceId",
                        column: x => x.ChildResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRelations_Resources_ParentResourceId",
                        column: x => x.ParentResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RuleResources",
                columns: table => new
                {
                    RuleId = table.Column<long>(type: "bigint", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleResources", x => new { x.RuleId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_RuleResources_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RuleResources_Rules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "Rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusyEventResources_ResourceId_BusyEventId",
                table: "BusyEventResources",
                columns: new[] { "ResourceId", "BusyEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_BusyEvents_StartUtc_EndUtc",
                table: "BusyEvents",
                columns: new[] { "StartUtc", "EndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProperties_ParentId",
                table: "ResourceProperties",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourcePropertyLinks_PropertyId_ResourceId",
                table: "ResourcePropertyLinks",
                columns: new[] { "PropertyId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRelations_ChildResourceId",
                table: "ResourceRelations",
                column: "ChildResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Code",
                table: "Resources",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_IsSchedulable",
                table: "Resources",
                column: "IsSchedulable");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TypeId",
                table: "Resources",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypeProperties_PropertyDefinitionId_ResourceTypeId",
                table: "ResourceTypeProperties",
                columns: new[] { "PropertyDefinitionId", "ResourceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_Key",
                table: "ResourceTypes",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_RuleResources_ResourceId_RuleId",
                table: "RuleResources",
                columns: new[] { "ResourceId", "RuleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_FromDateUtc_ToDateUtc_SingleDateUtc",
                table: "Rules",
                columns: new[] { "FromDateUtc", "ToDateUtc", "SingleDateUtc" })
                .Annotation("SqlServer:Include", new[] { "IsExclude" });
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
        }
    }
}
