# Single-Table Data Model

All domain entities share one DynamoDB table (`{project_name}-app`) with a
composite primary key (`PK` + `SK`) and one GSI (`GSI1`).

## Key Design

| Entity      | PK                    | SK                           | GSI1PK          | GSI1SK              |
|-------------|-----------------------|------------------------------|-----------------|---------------------|
| Community   | `COMMUNITY#default`   | `#CONFIG`                    | —               | —                   |
| Person      | `PERSON#{personId}`   | `#PROFILE`                   | —               | —                   |
| Team        | `TEAM#{teamId}`       | `#PROFILE`                   | `TEAMS`         | `TEAM#{teamId}`     |
| Membership  | `PERSON#{personId}`   | `MEMBERSHIP#ACTIVE`          | `TEAM#{teamId}` | `MEMBER#{personId}` |
| Observation | `PERSON#{personId}`   | `OBSERVE#{teamId}`           | —               | —                   |
| Transfer    | `PERSON#{personId}`   | `TRANSFER#{transferId}`      | —               | —                   |
| Role        | `TEAM#{teamId}`       | `ROLE#{roleName}#{personId}` | —               | —                   |

### One active membership per person

`SK = MEMBERSHIP#ACTIVE` is a fixed literal. DynamoDB enforces uniqueness on
`PK + SK`, so a person can hold at most one `MEMBERSHIP#ACTIVE` item at a
time. To transfer teams, overwrite the item in a `TransactWriteItems` call
alongside the new Transfer record—no scan or conditional expression required.

## Access Patterns

| Pattern                              | Operation                          | Key condition                                |
|--------------------------------------|------------------------------------|----------------------------------------------|
| Get community config                 | GetItem                            | `PK = "COMMUNITY#default"`, `SK = "#CONFIG"` |
| List all teams                       | GSI1 Query                         | `GSI1PK = "TEAMS"`                           |
| Get team profile                     | GetItem                            | `PK = "TEAM#{id}"`, `SK = "#PROFILE"`        |
| Get team members                     | GSI1 Query                         | `GSI1PK = "TEAM#{id}"`, `GSI1SK begins_with "MEMBER#"` |
| Get team roles                       | Query                              | `PK = "TEAM#{id}"`, `SK begins_with "ROLE#"` |
| Get person profile                   | GetItem                            | `PK = "PERSON#{id}"`, `SK = "#PROFILE"`      |
| Get person's active team             | GetItem                            | `PK = "PERSON#{id}"`, `SK = "MEMBERSHIP#ACTIVE"` |
| Get person's observed teams          | Query                              | `PK = "PERSON#{id}"`, `SK begins_with "OBSERVE#"` |
| Get person's transfers               | Query                              | `PK = "PERSON#{id}"`, `SK begins_with "TRANSFER#"` |

### GSI justification

`GSI1` exists solely to support **team-centric membership lookups** (who are
the members of a given team?). Without it, the only alternative is a full-table
scan. All other access patterns hit the main table directly. No additional GSIs
are needed for the MVP screens.

## Item Shapes

### Community

There is exactly one community item (singleton). `TransferApprovalDefault`
controls whether team transfers require admin approval by default.

```json
{
  "PK":                      "COMMUNITY#default",
  "SK":                      "#CONFIG",
  "Name":                    "My Community",
  "Description":             "A self-assembling team of teams",
  "TransferApprovalDefault": "NOT_REQUIRED",
  "CreatedAt":               "2024-01-01T00:00:00Z",
  "UpdatedAt":               "2024-01-20T15:00:00Z"
}
```

`TransferApprovalDefault` values: `NOT_REQUIRED`, `REQUIRED`.

### Person

`IsAdmin` is an optional boolean. When `true`, the person may perform
admin-only actions (edit community config, delete abandoned teams). Admins
are designated manually via seed data or direct DynamoDB writes for the MVP.

```json
{
  "PK":        "PERSON#p1",
  "SK":        "#PROFILE",
  "PersonId":  "p1",
  "Name":      "Alice Smith",
  "Email":     "alice@example.com",
  "Skills":    ["Kubernetes", "Terraform"],
  "IsAdmin":   true,
  "CreatedAt": "2024-01-15T09:00:00Z"
}
```

### Team

```json
{
  "PK":            "TEAM#t1",
  "SK":            "#PROFILE",
  "GSI1PK":        "TEAMS",
  "GSI1SK":        "TEAM#t1",
  "TeamId":        "t1",
  "Name":          "Platform",
  "Purpose":       "Build shared infrastructure for product teams",
  "Mission":       "Enable every team to ship faster with less operational toil",
  "Skills":        ["Kubernetes", "AWS", "Terraform"],
  "OpenPositions": ["Security Champion", "Documentation Champion"],
  "CreatedAt":     "2024-01-10T08:00:00Z"
}
```

`OpenPositions` is a list of role names the team is actively seeking. An
empty list means the team is fully staffed.

### Membership

```json
{
  "PK":       "PERSON#p1",
  "SK":       "MEMBERSHIP#ACTIVE",
  "GSI1PK":  "TEAM#t1",
  "GSI1SK":  "MEMBER#p1",
  "PersonId": "p1",
  "TeamId":   "t1",
  "JoinedAt": "2024-01-15T09:30:00Z"
}
```

### Observation

```json
{
  "PK":       "PERSON#p1",
  "SK":       "OBSERVE#t2",
  "PersonId": "p1",
  "TeamId":   "t2",
  "Since":    "2024-01-20T10:00:00Z"
}
```

### Transfer

```json
{
  "PK":          "PERSON#p1",
  "SK":          "TRANSFER#tr1",
  "TransferId":  "tr1",
  "PersonId":    "p1",
  "FromTeamId":  "t1",
  "ToTeamId":    "t2",
  "Status":      "PENDING",
  "RequestedAt": "2024-02-01T09:00:00Z"
}
```

`Status` values: `PENDING`, `APPROVED`, `REJECTED`, `CANCELLED`.

### Role

```json
{
  "PK":         "TEAM#t1",
  "SK":         "ROLE#DRI#p1",
  "TeamId":     "t1",
  "PersonId":   "p1",
  "RoleName":   "DRI",
  "AssignedAt": "2024-01-16T10:00:00Z"
}
```

Role items live under the team's PK so a single `Query` on
`PK = "TEAM#{id}"` with `SK begins_with "ROLE#"` returns all role
assignments for that team. Example role names: `DRI`, `Quality Champion`,
`Security Champion`, `Accessibility Champion`, `Documentation Champion`,
`Operations Champion`.
