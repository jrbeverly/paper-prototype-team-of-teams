using Amazon.DynamoDBv2.Model;

namespace Backend;

static class ObservationsEndpoints
{
    public static void MapObservations(this WebApplication app)
    {
        app.MapPost("/teams/{id}/observe", ObserveTeam).RequireAuthorization();
        app.MapDelete("/teams/{id}/observe", UnobserveTeam).RequireAuthorization();
        app.MapGet("/me/observations", GetMyObservations).RequireAuthorization();
    }

    static async Task<IResult> ObserveTeam(string id, HttpContext ctx, TableClient db)
    {
        var personId = ctx.User.FindFirst("sub")?.Value;
        if (personId is null) return Results.Unauthorized();

        var team = await db.GetAsync($"TEAM#{id}", "#PROFILE");
        if (team is null) return Results.NotFound();

        var existing = await db.GetAsync($"PERSON#{personId}", $"OBSERVE#{id}");
        if (existing is not null)
            return Results.Ok(ToObservation(existing));

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
        var attrs = new Dictionary<string, AttributeValue>
        {
            ["PersonId"] = new(personId),
            ["TeamId"] = new(id),
            ["Since"] = new(now),
        };

        await db.PutAsync($"PERSON#{personId}", $"OBSERVE#{id}", attrs);
        return Results.Ok(ToObservation(attrs));
    }

    static async Task<IResult> UnobserveTeam(string id, HttpContext ctx, TableClient db)
    {
        var personId = ctx.User.FindFirst("sub")?.Value;
        if (personId is null) return Results.Unauthorized();

        var existing = await db.GetAsync($"PERSON#{personId}", $"OBSERVE#{id}");
        if (existing is null)
            return Results.NotFound(new { error = "Not observing this team" });

        await db.DeleteAsync($"PERSON#{personId}", $"OBSERVE#{id}");
        return Results.NoContent();
    }

    static async Task<IResult> GetMyObservations(HttpContext ctx, TableClient db)
    {
        var personId = ctx.User.FindFirst("sub")?.Value;
        if (personId is null) return Results.Unauthorized();

        var items = await db.QueryAsync($"PERSON#{personId}", "OBSERVE#");
        return Results.Ok(items.Select(ToObservation).ToList());
    }

    static ObservationRecord ToObservation(Dictionary<string, AttributeValue> attrs) =>
        new(attrs["TeamId"].S, attrs["Since"].S);
}

record ObservationRecord(string TeamId, string Since);
