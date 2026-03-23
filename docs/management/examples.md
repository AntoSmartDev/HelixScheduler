# Management Examples

## Goal
These examples show realistic end-to-end flows across the management layer. They are intentionally
small and focus on sequence, not on every available field.

Unless stated otherwise:
- the examples target the default tenant
- numeric IDs are illustrative placeholders returned by previous successful commands
- when working outside the default tenant, add `X-Helix-Tenant` or `X-Tenant`

## Example 1: Start From Zero

### Step 1. Create A Tenant
Request:

```http
POST /api/management/tenants
Content-Type: application/json

{
  "key": "clinic-a",
  "label": "Clinic A"
}
```

### Step 2. Create A Resource Type
```http
POST /api/management/resource-types
Content-Type: application/json

{
  "key": "room",
  "label": "Room",
  "sortOrder": 1
}
```

### Step 3. Create A Resource
```http
POST /api/management/resources
Content-Type: application/json

{
  "code": "ROOM-A",
  "name": "Room A",
  "isSchedulable": true,
  "capacity": 1,
  "typeId": 1
}
```

### Step 4. Add Type-Aware Property Compatibility
```http
POST /api/management/resource-types/property-definitions/assign
Content-Type: application/json

{
  "resourceTypeId": 1,
  "propertyDefinitionIds": [100]
}
```

### Step 5. Assign Properties To The Resource
```http
POST /api/management/resource-property-assignments/assign
Content-Type: application/json

{
  "resourceId": 10,
  "propertyIds": [100]
}
```

### Step 6. Create A Rule
```http
POST /api/management/rules
Content-Type: application/json

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

### Step 7. Register A Busy Event
```http
POST /api/management/busy-events
Content-Type: application/json

{
  "definition": {
    "resourceIds": [10],
    "startUtc": "2026-03-25T10:00:00Z",
    "endUtc": "2026-03-25T10:30:00Z",
    "title": "Occupied slot",
    "eventType": "Meeting",
    "externalKey": "ext-1"
  }
}
```

### Step 8. Validate The Tenant
```http
GET /api/management/validation/tenant
```

### Step 9. Inspect The Catalog Snapshot
```http
GET /api/management/catalog/snapshot
```

At this point the model is governed, readable and ready to support the compute layer.

### Step 10. Hand Off To Compute
```http
POST /api/availability/compute
Content-Type: application/json

{
  "fromDate": "2026-03-25",
  "toDate": "2026-03-25",
  "requiredResourceIds": [10],
  "slotDurationMinutes": 30,
  "includeRemainderSlot": false
}
```

This is the transition point between:
- management layer as model governance
- compute layer as availability calculation

## Example 2: External Busy Synchronization
Use the bulk or idempotent busy-event endpoints when an external system owns the busy-event
identity.

### Bulk Register
```http
POST /api/management/busy-events/bulk
Content-Type: application/json

{
  "definitions": [
    {
      "resourceIds": [10],
      "startUtc": "2026-03-27T08:00:00Z",
      "endUtc": "2026-03-27T09:00:00Z",
      "title": "Bulk 1",
      "eventType": "Sync",
      "externalKey": "ext-1"
    },
    {
      "resourceIds": [10, 11],
      "startUtc": "2026-03-27T09:00:00Z",
      "endUtc": "2026-03-27T10:00:00Z",
      "title": "Bulk 2",
      "eventType": "Sync",
      "externalKey": "ext-2"
    }
  ]
}
```

### Idempotent Upsert
```http
PUT /api/management/busy-events/upsert
Content-Type: application/json

{
  "definition": {
    "resourceIds": [10],
    "startUtc": "2026-03-27T08:00:00Z",
    "endUtc": "2026-03-27T09:30:00Z",
    "title": "Bulk 1 updated",
    "eventType": "Sync",
    "externalKey": "ext-1"
  }
}
```

Use this flow when retries are expected and the external system owns the record identity.

## Example 3: Legacy Consistency Remediation

### Inspect Legacy Inconsistencies
```http
GET /api/management/validation/legacy
```

Representative response:

```json
{
  "validation": {
    "isValid": false,
    "findings": [
      {
        "code": "validation.resource.assigned-property-inactive",
        "category": "InvalidOperation",
        "message": "Resource '10' references inactive property '200'.",
        "target": "resourceProperties"
      }
    ]
  },
  "repairPreview": {
    "inactiveResourcePropertyAssignments": [
      {
        "resourceId": 10,
        "propertyId": 200
      }
    ],
    "inactiveResourceTypePropertyMappings": [
      {
        "resourceTypeId": 1,
        "propertyId": 200
      }
    ],
    "totalRepairableItems": 2
  }
}
```

### Execute Explicit Cleanup
```http
POST /api/management/validation/legacy/cleanup-inactive-property-references
```

This command is intentionally explicit. Legacy repair is not hidden inside normal management flows.

## Example 4: Troubleshooting A Resource
When a resource behaves unexpectedly, use the resource configuration snapshot rather than guessing
from the database.

```http
POST /api/management/catalog/resource-configuration
Content-Type: application/json

{
  "resourceId": 10,
  "fromDateUtc": "2026-03-01",
  "toDateUtc": "2026-03-31"
}
```

This keeps management, read visibility and compute concerns properly separated.

## Example 5: Non-Default Tenant
When you need to work outside the default tenant, add a tenant header explicitly.

```http
GET /api/management/catalog/snapshot
X-Helix-Tenant: clinic-a
```

If the requested tenant does not exist, the WebApi fails before controller execution with `404`.
