# Management WebApi

## Purpose
The management WebApi is the HTTP adapter above the management layer.

Base path:

- `/api/management/*`

It exposes the same governance capabilities already available through the application layer. The
WebApi is an adapter, not the center of the design.

## Tenant Resolution
The management HTTP surface is tenant-aware.

Tenant resolution headers:
- `X-Helix-Tenant`
- `X-Tenant`

Resolution behavior:
- if no tenant header is provided, the default tenant is used
- if a non-default tenant header is provided and the tenant does not exist, the request fails with
  `404`

In practice, examples in this document omit tenant headers for brevity. Add one of the headers
above whenever you target a tenant other than `default`.

## Endpoint Groups

### Tenants
Base path:

- `/api/management/tenants`

Endpoints:
- `POST /api/management/tenants`
- `PUT /api/management/tenants`
- `GET /api/management/tenants`
- `GET /api/management/tenants/{tenantId}`
- `POST /api/management/tenants/{tenantId}/activate`
- `POST /api/management/tenants/{tenantId}/deactivate`

### Resource Catalog
Base path:

- `/api/management`

Endpoints:
- `POST /api/management/resource-types`
- `PUT /api/management/resource-types`
- `GET /api/management/resource-types`
- `GET /api/management/resource-types/{resourceTypeId}`
- `POST /api/management/resource-types/{resourceTypeId}/activate`
- `POST /api/management/resource-types/{resourceTypeId}/deactivate`
- `POST /api/management/resource-types/property-definitions/assign`
- `POST /api/management/resource-types/property-definitions/remove`
- `GET /api/management/resource-types/{resourceTypeId}/property-definitions`
- `POST /api/management/resources`
- `PUT /api/management/resources`
- `GET /api/management/resources`
- `GET /api/management/resources/{resourceId}`
- `POST /api/management/resources/{resourceId}/activate`
- `POST /api/management/resources/{resourceId}/deactivate`
- `POST /api/management/resources/{resourceId}/archive`

### Hierarchy
Endpoints:
- `POST /api/management/hierarchy/relations`
- `DELETE /api/management/hierarchy/relations`
- `GET /api/management/hierarchy/relations`

### Properties And Assignments
Endpoints:
- `POST /api/management/properties`
- `PUT /api/management/properties`
- `GET /api/management/properties`
- `GET /api/management/properties/{propertyId}`
- `POST /api/management/properties/{propertyId}/activate`
- `POST /api/management/properties/{propertyId}/deactivate`
- `POST /api/management/properties/relations`
- `DELETE /api/management/properties/relations`
- `GET /api/management/properties/relations`
- `POST /api/management/resource-property-assignments/assign`
- `POST /api/management/resource-property-assignments/remove`
- `GET /api/management/resource-property-assignments/{resourceId}`

### Rules
Base path:

- `/api/management/rules`

Endpoints:
- `POST /api/management/rules`
- `PUT /api/management/rules`
- `GET /api/management/rules`
- `GET /api/management/rules/{ruleId}`
- `POST /api/management/rules/{ruleId}/deactivate`

### Busy Events
Base path:

- `/api/management/busy-events`

Endpoints:
- `POST /api/management/busy-events`
- `POST /api/management/busy-events/bulk`
- `PUT /api/management/busy-events/upsert`
- `PUT /api/management/busy-events`
- `GET /api/management/busy-events`
- `GET /api/management/busy-events/{busyEventId}`
- `POST /api/management/busy-events/{busyEventId}/cancel`

### Catalog Read, Validation And Legacy Diagnostics
Endpoints:
- `GET /api/management/catalog/snapshot`
- `POST /api/management/catalog/resource-configuration`
- `GET /api/management/validation/tenant`
- `GET /api/management/validation/resources/{resourceId}`
- `GET /api/management/validation/legacy`
- `POST /api/management/validation/legacy/cleanup-inactive-property-references`

## HTTP Semantics

### Management Commands
Command endpoints return success payloads on success and a structured error payload on failure.

Error payload shape:

```json
{
  "errors": [
    {
      "code": "validation.sample",
      "category": "Validation",
      "message": "Representative management error example.",
      "target": "sample"
    }
  ]
}
```

Status mapping:
- `400` for validation failures
- `404` for not found
- `409` for conflict or invalid operation

### Read And Validation Endpoints
The management HTTP surface also includes read and diagnostic endpoints.

Important distinction:
- catalog snapshots are read oriented
- validation endpoints are diagnostic
- legacy cleanup is an explicit remediation command

Validation endpoints intentionally return `200` with findings even when the model is invalid.

### Tenant Failures
Tenant resolution happens before controller execution.

Representative tenant-not-found response:

```json
{
  "error": "Tenant not found.",
  "tenant": "missing-tenant"
}
```

## Example Commands

The IDs used in the examples below are illustrative placeholders. In real flows they come from the
success payloads returned by previous management commands.

### Create A Tenant
```json
{
  "key": "clinic-a",
  "label": "Clinic A"
}
```

### Create A Resource Type
```json
{
  "key": "room",
  "label": "Room",
  "sortOrder": 1
}
```

### Create A Resource
```json
{
  "code": "ROOM-A",
  "name": "Room A",
  "isSchedulable": true,
  "capacity": 1,
  "typeId": 1
}
```

### Assign Property Definitions To A Resource Type
```json
{
  "resourceTypeId": 1,
  "propertyDefinitionIds": [100, 200]
}
```

### Assign Properties To A Resource
```json
{
  "resourceId": 10,
  "propertyIds": [100]
}
```

### Create A Rule
```json
{
  "definition": {
    "shape": 2,
    "isExclude": false,
    "resourceIds": [10],
    "title": "Morning shift",
    "singleDateUtc": "2026-03-25",
    "startTime": "09:00:00",
    "endTime": "12:00:00"
  }
}
```

### Register A Busy Event
```json
{
  "definition": {
    "resourceIds": [10, 11],
    "startUtc": "2026-03-25T12:00:00Z",
    "endUtc": "2026-03-25T13:00:00Z",
    "title": "Team sync",
    "eventType": "Meeting",
    "externalKey": "ext-123"
  }
}
```

### Request A Resource Configuration Snapshot
```json
{
  "resourceId": 10,
  "fromDateUtc": "2026-03-01",
  "toDateUtc": "2026-03-31"
}
```

## OpenAPI
The WebApi exposes an OpenAPI document when OpenAPI exposure is enabled.

See:
- `/openapi/v1.json`

Default behavior:
- enabled in `Development`
- disabled in other environments unless `OpenApi:Expose=true`

The OpenAPI document includes:
- management-layer descriptions
- endpoint summaries
- selected request and response examples

Use it as the authoritative HTTP reference for payload details.
