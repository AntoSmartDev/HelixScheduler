# API Examples

This document is the canonical public source for HTTP examples against the current HelixScheduler Web API surface.

Examples use UTC dates and the default tenant unless an explicit tenant header is shown.
The API accepts either `X-Tenant` or `X-Helix-Tenant`. If no tenant header is provided, the Web API resolves or creates the `default` tenant.

## Surface summary

- `POST /api/availability/compute` is the full availability surface.
  It supports property filter groups, ancestor filters, slot chunking, explanation output, and resource OR groups.
- `GET /api/availability/slots` is the minimal query-string adapter.
  It supports date range, `resourceIds`, `orGroups`, `explain`, and ancestor expansion only.
- `GET /api/catalog/*` exposes the canonical read-only catalog endpoints.
- `GET /api/resources` is a lightweight compatibility alias over the resource catalog.
  Prefer `/api/catalog/resources` in public integrations unless you explicitly want the summary-only projection.
- `GET /health` is the monitoring and readiness endpoint.
- Property filters are compute-only.
  Use `propertyFilterGroups` in the POST body. The GET endpoint does not support `propertyIds` or `includePropertyDescendants`.
- `/api/demo/*` and `/api/diag/*` exist for demo and diagnostics support.
  They are not part of the main public HTTP surface described in this document.

## Availability compute (basic)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-03-09",
    "toDate": "2026-03-09",
    "requiredResourceIds": [5, 4]
  }'
```

## Availability compute (explain)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-03-09",
    "toDate": "2026-03-09",
    "requiredResourceIds": [5, 4],
    "explain": true
  }'
```

## Availability compute (chunked slots)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-03-09",
    "toDate": "2026-03-09",
    "requiredResourceIds": [5, 4],
    "slotDurationMinutes": 60,
    "includeRemainderSlot": false
  }'
```

## Availability compute (chunked with remainder)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-03-12",
    "toDate": "2026-03-12",
    "requiredResourceIds": [4],
    "slotDurationMinutes": 60,
    "includeRemainderSlot": true
  }'
```

## Availability compute (resource ancestors)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-01-06",
    "toDate": "2026-01-06",
    "requiredResourceIds": [2, 3],
    "includeResourceAncestors": true,
    "ancestorRelationTypes": ["Contains"],
    "ancestorMode": "perGroup"
  }'
```

## Ancestor filters primer

- `matchMode: "or"` means at least one `propertyId` must match.
- `matchMode: "and"` means all `propertyIds` in the filter must match.
- `scope` can be `anyAncestor`, `directParent`, or `nearestOfType`.
- `matchAllAncestors: true` requires every eligible ancestor to satisfy the filter.

## Availability compute (ancestor filters)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-01-06",
    "toDate": "2026-01-06",
    "requiredResourceIds": [301],
    "includeResourceAncestors": true,
    "ancestorFilters": [
      {
        "resourceTypeId": 1,
        "propertyIds": [101, 102],
        "matchMode": "or",
        "scope": "nearestOfType"
      }
    ]
  }'
```

## Availability compute (ancestor filters with matchAllAncestors)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-01-06",
    "toDate": "2026-01-06",
    "requiredResourceIds": [301],
    "includeResourceAncestors": true,
    "ancestorFilters": [
      {
        "resourceTypeId": 3,
        "propertyIds": [201, 202],
        "matchMode": "and",
        "scope": "directParent",
        "matchAllAncestors": true
      }
    ]
  }'
```

## Property filter groups primer

- Use `propertyFilterGroups` only with `POST /api/availability/compute`.
- `matchMode` is evaluated inside each group.
- Groups are combined with logical AND.
- `includePropertyDescendants` applies per group.

## Availability compute (single property filter group)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-01-06",
    "toDate": "2026-01-06",
    "requiredResourceIds": [2, 3],
    "propertyFilterGroups": [
      {
        "propertyIds": [3],
        "matchMode": "and"
      }
    ]
  }'
```

## Availability compute (property filter groups with descendants)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-01-06",
    "toDate": "2026-01-06",
    "requiredResourceIds": [2, 3],
    "propertyFilterGroups": [
      {
        "propertyIds": [3],
        "matchMode": "and",
        "includePropertyDescendants": true
      }
    ]
  }'
```

## Availability compute (multiple property filter groups)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-01-06",
    "toDate": "2026-01-06",
    "requiredResourceIds": [2, 3],
    "propertyFilterGroups": [
      {
        "propertyIds": [3],
        "matchMode": "and"
      },
      {
        "propertyIds": [7, 8],
        "matchMode": "or"
      }
    ]
  }'
```

## Availability compute (OR groups)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-03-09",
    "toDate": "2026-03-09",
    "requiredResourceIds": [5],
    "resourceOrGroups": [[4, 6], [7, 8]]
  }'
```

## Availability compute (OR groups plus property filters)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2026-03-09",
    "toDate": "2026-03-09",
    "requiredResourceIds": [5],
    "resourceOrGroups": [[4, 6]],
    "propertyFilterGroups": [
      {
        "propertyIds": [3],
        "matchMode": "and",
        "includePropertyDescendants": true
      }
    ]
  }'
```

## Availability compute (explicit tenant header)
```bash
curl -X POST http://localhost:5000/api/availability/compute \
  -H "Content-Type: application/json" \
  -H "X-Tenant: default" \
  -d '{
    "fromDate": "2026-03-09",
    "toDate": "2026-03-09",
    "requiredResourceIds": [5, 4]
  }'
```

## Availability slots (GET)
```bash
curl "http://localhost:5000/api/availability/slots?fromDate=2026-01-06&toDate=2026-01-06&resourceIds=2,3"
```

## Availability slots (GET with explain)
```bash
curl "http://localhost:5000/api/availability/slots?fromDate=2026-01-06&toDate=2026-01-06&resourceIds=2,3&explain=true"
```

## Availability slots (GET with ancestors)
```bash
curl "http://localhost:5000/api/availability/slots?fromDate=2026-01-06&toDate=2026-01-06&resourceIds=2,3&includeResourceAncestors=true&ancestorMode=perGroup&ancestorRelationTypes=Contains"
```

## Availability slots (GET with OR groups)
```bash
curl "http://localhost:5000/api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5&orGroups=4,6|7,8"
```

## Catalog endpoints
```bash
curl "http://localhost:5000/api/catalog/resource-types"
curl "http://localhost:5000/api/catalog/resources?onlySchedulable=true"
curl "http://localhost:5000/api/catalog/properties"
```

