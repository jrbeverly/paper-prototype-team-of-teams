#!/usr/bin/env bash
# Seed the local DynamoDB table with sample data for development.
# Requires AWS CLI and a running DynamoDB Local instance.
#
# Usage:
#   bash scripts/seed-local.sh
#
# Env vars:
#   DYNAMODB_ENDPOINT  defaults to http://localhost:4566
#   TABLE_NAME         defaults to team-of-teams-app
set -euo pipefail

ENDPOINT="${DYNAMODB_ENDPOINT:-http://localhost:4566}"
TABLE="${TABLE_NAME:-team-of-teams-app}"

put() {
  aws dynamodb put-item \
    --endpoint-url "$ENDPOINT" \
    --region us-east-1 \
    --no-cli-pager \
    --table-name "$TABLE" \
    --item "$1"
}

echo "Seeding '$TABLE' at $ENDPOINT"

# ---------------------------------------------------------------------------
# Community (singleton)
# ---------------------------------------------------------------------------

put '{
  "PK":                      {"S": "COMMUNITY#default"},
  "SK":                      {"S": "#CONFIG"},
  "Name":                    {"S": "My Community"},
  "Description":             {"S": "A self-assembling team of teams"},
  "TransferApprovalDefault": {"S": "NOT_REQUIRED"},
  "CreatedAt":               {"S": "2024-01-01T00:00:00Z"}
}'

# ---------------------------------------------------------------------------
# People
# ---------------------------------------------------------------------------

put '{
  "PK":        {"S": "PERSON#p1"},
  "SK":        {"S": "#PROFILE"},
  "PersonId":  {"S": "p1"},
  "Name":      {"S": "Alice Smith"},
  "Email":     {"S": "alice@example.com"},
  "Skills":    {"L": [{"S": "Kubernetes"}, {"S": "Terraform"}, {"S": "AWS"}]},
  "IsAdmin":   {"BOOL": true},
  "CreatedAt": {"S": "2024-01-15T09:00:00Z"}
}'

put '{
  "PK":        {"S": "PERSON#p2"},
  "SK":        {"S": "#PROFILE"},
  "PersonId":  {"S": "p2"},
  "Name":      {"S": "Bob Chen"},
  "Email":     {"S": "bob@example.com"},
  "Skills":    {"L": [{"S": "Vue.js"}, {"S": "TypeScript"}, {"S": "CSS"}]},
  "CreatedAt": {"S": "2024-01-16T10:00:00Z"}
}'

# ---------------------------------------------------------------------------
# Teams
# ---------------------------------------------------------------------------

put '{
  "PK":            {"S": "TEAM#t1"},
  "SK":            {"S": "#PROFILE"},
  "GSI1PK":        {"S": "TEAMS"},
  "GSI1SK":        {"S": "TEAM#t1"},
  "TeamId":        {"S": "t1"},
  "Name":          {"S": "Platform"},
  "Purpose":       {"S": "Build shared infrastructure for all product teams"},
  "Mission":       {"S": "Enable every team to ship faster with less operational toil"},
  "Skills":        {"L": [{"S": "Kubernetes"}, {"S": "AWS"}, {"S": "Terraform"}]},
  "OpenPositions": {"L": [{"S": "Security Champion"}, {"S": "Documentation Champion"}]},
  "CreatedAt":     {"S": "2024-01-10T08:00:00Z"}
}'

put '{
  "PK":            {"S": "TEAM#t2"},
  "SK":            {"S": "#PROFILE"},
  "GSI1PK":        {"S": "TEAMS"},
  "GSI1SK":        {"S": "TEAM#t2"},
  "TeamId":        {"S": "t2"},
  "Name":          {"S": "Product"},
  "Purpose":       {"S": "Design and deliver the core user experience"},
  "Mission":       {"S": "Make team discovery and self-assembly feel effortless"},
  "Skills":        {"L": [{"S": "Vue.js"}, {"S": "UX Design"}, {"S": "TypeScript"}]},
  "OpenPositions": {"L": []},
  "CreatedAt":     {"S": "2024-01-11T08:00:00Z"}
}'

# ---------------------------------------------------------------------------
# Memberships  (SK = MEMBERSHIP#ACTIVE enforces one active team per person)
# ---------------------------------------------------------------------------

put '{
  "PK":       {"S": "PERSON#p1"},
  "SK":       {"S": "MEMBERSHIP#ACTIVE"},
  "GSI1PK":  {"S": "TEAM#t1"},
  "GSI1SK":  {"S": "MEMBER#p1"},
  "PersonId": {"S": "p1"},
  "TeamId":   {"S": "t1"},
  "JoinedAt": {"S": "2024-01-15T09:30:00Z"}
}'

put '{
  "PK":       {"S": "PERSON#p2"},
  "SK":       {"S": "MEMBERSHIP#ACTIVE"},
  "GSI1PK":  {"S": "TEAM#t2"},
  "GSI1SK":  {"S": "MEMBER#p2"},
  "PersonId": {"S": "p2"},
  "TeamId":   {"S": "t2"},
  "JoinedAt": {"S": "2024-01-16T11:00:00Z"}
}'

# ---------------------------------------------------------------------------
# Observations
# ---------------------------------------------------------------------------

put '{
  "PK":       {"S": "PERSON#p1"},
  "SK":       {"S": "OBSERVE#t2"},
  "PersonId": {"S": "p1"},
  "TeamId":   {"S": "t2"},
  "Since":    {"S": "2024-01-20T10:00:00Z"}
}'

put '{
  "PK":       {"S": "PERSON#p2"},
  "SK":       {"S": "OBSERVE#t1"},
  "PersonId": {"S": "p2"},
  "TeamId":   {"S": "t1"},
  "Since":    {"S": "2024-01-21T14:00:00Z"}
}'

# ---------------------------------------------------------------------------
# Transfer (Bob requesting to move from Product to Platform)
# ---------------------------------------------------------------------------

put '{
  "PK":          {"S": "PERSON#p2"},
  "SK":          {"S": "TRANSFER#tr1"},
  "TransferId":  {"S": "tr1"},
  "PersonId":    {"S": "p2"},
  "FromTeamId":  {"S": "t2"},
  "ToTeamId":    {"S": "t1"},
  "Status":      {"S": "PENDING"},
  "RequestedAt": {"S": "2024-02-01T09:00:00Z"}
}'

# ---------------------------------------------------------------------------
# Roles
# ---------------------------------------------------------------------------

put '{
  "PK":         {"S": "TEAM#t1"},
  "SK":         {"S": "ROLE#DRI#p1"},
  "TeamId":     {"S": "t1"},
  "PersonId":   {"S": "p1"},
  "RoleName":   {"S": "DRI"},
  "AssignedAt": {"S": "2024-01-16T10:00:00Z"}
}'

put '{
  "PK":         {"S": "TEAM#t2"},
  "SK":         {"S": "ROLE#Quality Champion#p2"},
  "TeamId":     {"S": "t2"},
  "PersonId":   {"S": "p2"},
  "RoleName":   {"S": "Quality Champion"},
  "AssignedAt": {"S": "2024-01-17T09:00:00Z"}
}'

echo "Done."
