using Amazon.DynamoDBv2.Model;

namespace Backend;

static class RolesEndpoints
{
    public static void MapRoles(this WebApplication app)
    {
        app.MapPost("/teams/{id}/roles", AssignRole).RequireAuthorization();
        app.MapDelete("/teams/{id}/roles/{role}", RemoveRole).RequireAuthorization();
    }

    static async Task<IResult> AssignRole(string id, AssignRoleRequest req, TableClient db)
    {
        if (string.IsNullOrWhiteSpace(req.RoleName))
            return Results.BadRequest(new { error = "RoleName is required" });
        if (string.IsNullOrWhiteSpace(req.PersonId))
            return Results.BadRequest(new { error = "PersonId is required" });

        var team = await db.GetAsync($"TEAM#{id}", "#PROFILE");
        if (team is null) return Results.NotFound();

        var membership = await db.GetAsync($"PERSON#{req.PersonId}", "MEMBERSHIP#ACTIVE");
        if (membership is null || membership["TeamId"].S != id)
            return Results.BadRequest(new { error = "Person is not an active member of this team" });

        var roleName = req.RoleName.Trim();
        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
        var attrs = new Dictionary<string, AttributeValue>
        {
            ["TeamId"] = new(id),
            ["PersonId"] = new(req.PersonId),
            ["RoleName"] = new(roleName),
            ["AssignedAt"] = new(now),
        };

        await db.PutAsync($"TEAM#{id}", $"ROLE#{roleName}#{req.PersonId}", attrs);
        return Results.Ok(new RoleRecord(req.PersonId, roleName, now));
    }

    static async Task<IResult> RemoveRole(string id, string role, string personId, TableClient db)
    {
        var item = await db.GetAsync($"TEAM#{id}", $"ROLE#{role}#{personId}");
        if (item is null) return Results.NotFound();

        await db.DeleteAsync($"TEAM#{id}", $"ROLE#{role}#{personId}");
        return Results.NoContent();
    }
}

record AssignRoleRequest(string RoleName, string PersonId);
record RoleRecord(string PersonId, string RoleName, string AssignedAt);
