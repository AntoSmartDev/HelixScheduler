# API Examples

These examples are minimal, version-agnostic, and use UTC.

## Ancestor filters primer
- matchMode: "or" = at least one propertyId; "and" = all propertyIds.
- scope: "anyAncestor" (default), "directParent", "nearestOfType".
- matchAllAncestors: false = at least one eligible ancestor matches; true = all eligible ancestors must match.

## Availability compute (basic)
```json
POST /api/availability/compute
{
  "fromDate": "2026-03-09",
  "toDate": "2026-03-09",
  "requiredResourceIds": [5,4]
}
```

## Availability compute (explain)
```json
POST /api/availability/compute
{
  "fromDate": "2026-03-09",
  "toDate": "2026-03-09",
  "requiredResourceIds": [5,4],
  "explain": true
}
```

## Availability compute (chunked slots)
```json
POST /api/availability/compute
{
  "fromDate": "2026-03-09",
  "toDate": "2026-03-09",
  "requiredResourceIds": [5,4],
  "slotDurationMinutes": 60,
  "includeRemainderSlot": false
}
```

## Availability compute (chunked with remainder)
```json
POST /api/availability/compute
{
  "fromDate": "2026-03-12",
  "toDate": "2026-03-12",
  "requiredResourceIds": [4],
  "slotDurationMinutes": 60,
  "includeRemainderSlot": true
}
```

## Availability compute (resource ancestors)
```json
POST /api/availability/compute
{
  "fromDate": "2026-01-06",
  "toDate": "2026-01-06",
  "requiredResourceIds": [2,3],
  "includeResourceAncestors": true,
  "ancestorRelationTypes": ["Contains"],
  "ancestorMode": "perGroup"
}
```

## Availability compute (ancestor filters)
```json
POST /api/availability/compute
{
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
}
```

## Availability compute (ancestor filters with matchAllAncestors)
```json
POST /api/availability/compute
{
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
}
```

## Availability compute (properties filter)
```json
POST /api/availability/compute
{
  "fromDate": "2026-01-06",
  "toDate": "2026-01-06",
  "requiredResourceIds": [2,3],
  "propertyIds": [3]
}
```

## Availability compute (properties with descendants)
```json
POST /api/availability/compute
{
  "fromDate": "2026-01-06",
  "toDate": "2026-01-06",
  "requiredResourceIds": [2,3],
  "propertyIds": [3],
  "includePropertyDescendants": true
}
```

## Availability compute (OR groups)
```json
POST /api/availability/compute
{
  "fromDate": "2026-03-09",
  "toDate": "2026-03-09",
  "requiredResourceIds": [5],
  "resourceOrGroups": [[4,6],[7,8]]
}
```

## Availability compute (OR groups + properties)
```json
POST /api/availability/compute
{
  "fromDate": "2026-03-09",
  "toDate": "2026-03-09",
  "requiredResourceIds": [5],
  "resourceOrGroups": [[4,6]],
  "propertyIds": [3],
  "includePropertyDescendants": true
}
```

## Availability slots (GET)
```
GET /api/availability/slots?fromDate=2026-01-06&toDate=2026-01-06&resourceIds=2,3
```

## Availability slots (GET with ancestors)
```
GET /api/availability/slots?fromDate=2026-01-06&toDate=2026-01-06&resourceIds=2,3&includeResourceAncestors=true&ancestorMode=perGroup&ancestorRelationTypes=Contains
```

## Availability slots (GET with OR groups)
```
GET /api/availability/slots?fromDate=2026-03-09&toDate=2026-03-09&resourceIds=5&orGroups=4,6|7,8
```

## Availability slots (GET with properties)
```
GET /api/availability/slots?fromDate=2026-01-06&toDate=2026-01-06&resourceIds=2,3&propertyIds=3
```

## Availability slots (GET with properties + descendants)
```
GET /api/availability/slots?fromDate=2026-01-06&toDate=2026-01-06&resourceIds=2,3&propertyIds=3&includePropertyDescendants=true
```

## Catalog endpoints
```
GET /api/catalog/resource-types
GET /api/catalog/resources?onlySchedulable=true
GET /api/catalog/properties
```
