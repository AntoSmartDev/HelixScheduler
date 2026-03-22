using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace HelixScheduler.WebApi.Management;

public static class ManagementOpenApiConfiguration
{
    public static void Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "HelixScheduler WebApi";
            document.Info.Description =
                """
                HelixScheduler exposes two official HTTP surfaces:

                - compute layer: `/api/availability/*`
                - management layer: `/api/management/*`

                The management layer follows a real `start from zero` journey:

                1. create or select a tenant
                2. create resource types
                3. create resources
                4. shape hierarchy and properties
                5. assign properties and type mappings
                6. configure rules and busy events
                7. inspect catalog snapshots and validation/legacy diagnostics

                Important boundaries:

                - management endpoints are command/governance oriented
                - catalog snapshots are read/troubleshooting oriented
                - validation and legacy diagnostics are not command failures by themselves
                """;

            document.Tags = new HashSet<OpenApiTag>
            {
                new OpenApiTag
                {
                    Name = "Management / Tenants",
                    Description = "Tenant lifecycle and administrative bootstrap."
                },
                new OpenApiTag
                {
                    Name = "Management / Resource Catalog",
                    Description = "Resource types, resources, hierarchy, properties and assignments."
                },
                new OpenApiTag
                {
                    Name = "Management / Rules",
                    Description = "Rule commands with compact shape-aware contracts."
                },
                new OpenApiTag
                {
                    Name = "Management / Busy Events",
                    Description = "Busy-event commands, including bulk and idempotent integration flows."
                },
                new OpenApiTag
                {
                    Name = "Management / Catalog Read And Validation",
                    Description = "Read snapshots, validation and legacy diagnostics."
                }
            };

            return Task.CompletedTask;
        });

        options.AddOperationTransformer(async (operation, context, ct) =>
        {
            var path = "/" + (context.Description.RelativePath?.Split('?')[0]?.TrimStart('/') ?? string.Empty);
            var method = context.Description.HttpMethod?.ToUpperInvariant() ?? string.Empty;

            if (!path.StartsWith("/api/management", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ApplySummaries(operation, method, path);
            ApplyExamples(operation, method, path);
            await ApplyFailureResponsesAsync(operation, context, ct).ConfigureAwait(false);
        });
    }

    private static void ApplySummaries(OpenApiOperation operation, string method, string path)
    {
        if (method == "POST" && path == "/api/management/tenants")
        {
            operation.Summary = "Create tenant";
            operation.Description = "Starts the management journey by creating a tenant lifecycle entry. This is governance state, not the technical tenant resolution contract.";
        }
        else if (method == "POST" && path == "/api/management/resource-types")
        {
            operation.Summary = "Create resource type";
            operation.Description = "Creates a catalog type that resources can reference. Type-aware property compatibility stays outside this endpoint.";
        }
        else if (method == "POST" && path == "/api/management/resources")
        {
            operation.Summary = "Create resource";
            operation.Description = "Creates a schedulable resource linked to an existing resource type. Resource lifecycle is handled later by activate/deactivate/archive commands.";
        }
        else if (method == "POST" && path == "/api/management/resource-types/property-definitions/assign")
        {
            operation.Summary = "Assign property definitions to resource type";
            operation.Description = "Updates the `ResourceType -> PropertyDefinition` management boundary. This governs mappings, while `PropertySchemaService` remains the canonical schema/read boundary.";
        }
        else if (method == "POST" && path == "/api/management/resource-property-assignments/assign")
        {
            operation.Summary = "Assign properties to resource";
            operation.Description = "Assigns catalog properties to a concrete resource. Type-aware compatibility is validated against the resource type.";
        }
        else if (method == "POST" && path == "/api/management/rules")
        {
            operation.Summary = "Create rule";
            operation.Description = "Adds a management rule using the compact shape-aware contract. The public surface stays unified across weekly, monthly, single-date, range and repeating rules.";
        }
        else if (method == "POST" && path == "/api/management/busy-events")
        {
            operation.Summary = "Register busy event";
            operation.Description = "Registers a management-level busy event for one or more resources. `BusySlot` remains an internal compute format, not a public contract.";
        }
        else if (method == "POST" && path == "/api/management/busy-events/bulk")
        {
            operation.Summary = "Register busy events in bulk";
            operation.Description = "Atomic bulk command for external integrations. Duplicate external keys inside the batch or against stored events are rejected coherently.";
        }
        else if (method == "PUT" && path == "/api/management/busy-events/upsert")
        {
            operation.Summary = "Upsert busy event by external key";
            operation.Description = "Idempotent integration endpoint keyed by `externalKey`. Use this when the external system owns the busy-event identity.";
        }
        else if (method == "GET" && path == "/api/management/catalog/snapshot")
        {
            operation.Summary = "Get scheduler catalog snapshot";
            operation.Description = "Read-oriented snapshot for onboarding and troubleshooting. This is not a management command and intentionally composes existing query services.";
        }
        else if (method == "POST" && path == "/api/management/catalog/resource-configuration")
        {
            operation.Summary = "Get resource configuration snapshot";
            operation.Description = "Troubleshooting snapshot for a single resource over a date window. Useful when validation or lifecycle state must be inspected together.";
        }
        else if (method == "GET" && path == "/api/management/validation/tenant")
        {
            operation.Summary = "Validate tenant model";
            operation.Description = "Returns explainable findings for the current tenant model. Invalid output still returns `200` because this endpoint is diagnostic, not a command.";
        }
        else if (method == "GET" && path == "/api/management/validation/legacy")
        {
            operation.Summary = "Get legacy consistency report";
            operation.Description = "Reports legacy inconsistencies that may be hidden by filtered read-side behavior, including repairable references to inactive properties.";
        }
        else if (method == "POST" && path == "/api/management/validation/legacy/cleanup-inactive-property-references")
        {
            operation.Summary = "Cleanup inactive property references";
            operation.Description = "Explicit remediation command that removes only the repairable legacy references to inactive properties. It does not auto-fix every legacy inconsistency.";
        }
    }

    private static void ApplyExamples(OpenApiOperation operation, string method, string path)
    {
        if (method == "POST" && path == "/api/management/tenants")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["key"] = "clinic-a",
                ["label"] = "Clinic A"
            });
        }
        else if (method == "POST" && path == "/api/management/resource-types")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["key"] = "room",
                ["label"] = "Room",
                ["sortOrder"] = 1
            });
        }
        else if (method == "POST" && path == "/api/management/resources")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["code"] = "ROOM-A",
                ["name"] = "Room A",
                ["isSchedulable"] = true,
                ["capacity"] = 1,
                ["typeId"] = 1
            });
        }
        else if (method == "POST" && path == "/api/management/resource-types/property-definitions/assign")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["resourceTypeId"] = 1,
                ["propertyDefinitionIds"] = new JsonArray
                {
                    100,
                    200
                }
            });
        }
        else if (method == "POST" && path == "/api/management/resource-property-assignments/assign")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["resourceId"] = 10,
                ["propertyIds"] = new JsonArray
                {
                    100
                }
            });
        }
        else if (method == "POST" && path == "/api/management/rules")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["definition"] = new JsonObject
                {
                    ["shape"] = 2,
                    ["isExclude"] = false,
                    ["resourceIds"] = new JsonArray { 10 },
                    ["title"] = "Morning shift",
                    ["singleDateUtc"] = "2026-03-25",
                    ["startTime"] = "09:00:00",
                    ["endTime"] = "12:00:00"
                }
            });
        }
        else if (method == "POST" && path == "/api/management/busy-events")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["definition"] = new JsonObject
                {
                    ["resourceIds"] = new JsonArray { 10, 11 },
                    ["startUtc"] = "2026-03-25T12:00:00Z",
                    ["endUtc"] = "2026-03-25T13:00:00Z",
                    ["title"] = "Team sync",
                    ["eventType"] = "Meeting",
                    ["externalKey"] = "ext-123"
                }
            });
        }
        else if (method == "POST" && path == "/api/management/busy-events/bulk")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["definitions"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["resourceIds"] = new JsonArray { 10 },
                        ["startUtc"] = "2026-03-27T08:00:00Z",
                        ["endUtc"] = "2026-03-27T09:00:00Z",
                        ["title"] = "Bulk 1",
                        ["eventType"] = "Sync",
                        ["externalKey"] = "ext-1"
                    },
                    new JsonObject
                    {
                        ["resourceIds"] = new JsonArray { 10, 11 },
                        ["startUtc"] = "2026-03-27T09:00:00Z",
                        ["endUtc"] = "2026-03-27T10:00:00Z",
                        ["title"] = "Bulk 2",
                        ["eventType"] = "Sync",
                        ["externalKey"] = "ext-2"
                    }
                }
            });
        }
        else if (method == "POST" && path == "/api/management/catalog/resource-configuration")
        {
            SetRequestExample(operation, new JsonObject
            {
                ["resourceId"] = 10,
                ["fromDateUtc"] = "2026-03-01",
                ["toDateUtc"] = "2026-03-31"
            });
        }

        if (method == "GET" && path == "/api/management/validation/legacy")
        {
            SetResponseExample(operation, "200", new JsonObject
            {
                ["validation"] = new JsonObject
                {
                    ["isValid"] = false,
                    ["findings"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["code"] = "validation.resource.assigned-property-inactive",
                            ["category"] = "InvalidOperation",
                            ["message"] = "Resource '10' references inactive property '200'.",
                            ["target"] = "resourceProperties"
                        }
                    }
                },
                ["repairPreview"] = new JsonObject
                {
                    ["inactiveResourcePropertyAssignments"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["resourceId"] = 10,
                            ["propertyId"] = 200
                        }
                    },
                    ["inactiveResourceTypePropertyMappings"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["resourceTypeId"] = 1,
                            ["propertyId"] = 200
                        }
                    },
                    ["totalRepairableItems"] = 2
                }
            });
        }
        else if (method == "POST" && path == "/api/management/validation/legacy/cleanup-inactive-property-references")
        {
            SetResponseExample(operation, "200", new JsonObject
            {
                ["removedResourcePropertyAssignments"] = 1,
                ["removedResourceTypePropertyMappings"] = 1,
                ["reportAfter"] = new JsonObject
                {
                    ["validation"] = new JsonObject
                    {
                        ["isValid"] = true,
                        ["findings"] = new JsonArray()
                    },
                    ["repairPreview"] = new JsonObject
                    {
                        ["inactiveResourcePropertyAssignments"] = new JsonArray(),
                        ["inactiveResourceTypePropertyMappings"] = new JsonArray(),
                        ["totalRepairableItems"] = 0
                    }
                }
            });
        }
    }

    private static async Task ApplyFailureResponsesAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        var schema = await context.GetOrCreateSchemaAsync(typeof(ManagementFailureResponse), null, ct).ConfigureAwait(false);

        AddFailureResponse(operation, "400", "Validation failure on the management command.", schema, "validation.sample");
        AddFailureResponse(operation, "404", "Referenced entity was not found for the current tenant.", schema, "not-found.sample");
        AddFailureResponse(operation, "409", "Conflict or invalid operation according to the management grammar.", schema, "conflict.sample");
    }

    private static void AddFailureResponse(OpenApiOperation operation, string statusCode, string description, IOpenApiSchema schema, string code)
    {
        operation.Responses ??= new OpenApiResponses();
        if (!operation.Responses.TryGetValue(statusCode, out var existingResponse) ||
            existingResponse is not OpenApiResponse response)
        {
            response = new OpenApiResponse();
            operation.Responses[statusCode] = response;
        }

        response.Description = description;
        response.Content ??= new Dictionary<string, OpenApiMediaType>();
        response.Content["application/json"] = new OpenApiMediaType
        {
            Schema = schema,
            Example = new JsonObject
            {
                ["errors"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["code"] = code,
                        ["category"] = statusCode == "404" ? "NotFound" : statusCode == "409" ? "Conflict" : "Validation",
                        ["message"] = "Representative management error example.",
                        ["target"] = "sample"
                    }
                }
            }
        };
    }

    private static void SetRequestExample(OpenApiOperation operation, JsonNode example)
    {
        if (operation.RequestBody?.Content == null)
        {
            return;
        }

        if (operation.RequestBody.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType.Example = example;
        }
    }

    private static void SetResponseExample(OpenApiOperation operation, string statusCode, JsonNode example)
    {
        if (operation.Responses == null ||
            !operation.Responses.TryGetValue(statusCode, out var response) ||
            response.Content == null)
        {
            return;
        }

        if (response.Content.TryGetValue("application/json", out var mediaType))
        {
            mediaType.Example = example;
        }
    }
}
