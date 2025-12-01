using Amazon.DynamoDBv2.Model;

namespace Backend;

static class MeEndpoints
{
    public static void MapMe(this WebApplication app)
    {
        app.MapGet("/me", GetMe).RequireAuthorization();
        app.MapPatch("/me", PatchMe).RequireAuthorization();
    }

    static async Task<IResult> GetMe(HttpContext ctx, TableClient db)
    {
        var (personId, email) = GetClaims(ctx);
        if (personId is null) return Results.Unauthorized();

        var item = await db.GetAsync($"PERSON#{personId}", "#PROFILE");
        if (item is null)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
            item = new Dictionary<string, AttributeValue>
            {
                ["PersonId"] = new(personId),
                ["Name"] = new(email ?? personId),
                ["Email"] = new(email ?? ""),
                ["CreatedAt"] = new(now),
            };
            SetList(item, "Skills", []);
            await db.PutAsync($"PERSON#{personId}", "#PROFILE", item);
        }

        return Results.Ok(ToPerson(item));
    }

    static async Task<IResult> PatchMe(PatchMeRequest req, HttpContext ctx, TableClient db)
    {
        var (personId, _) = GetClaims(ctx);
        if (personId is null) return Results.Unauthorized();

        var item = await db.GetAsync($"PERSON#{personId}", "#PROFILE");
        if (item is null) return Results.NotFound();

        if (req.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name cannot be blank" });
            item["Name"] = new(req.Name.Trim());
        }
        if (req.Bio is not null) item["Bio"] = new(req.Bio);
        if (req.Skills is not null) SetList(item, "Skills", req.Skills);
        item["UpdatedAt"] = new(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z");

        await db.PutAsync($"PERSON#{personId}", "#PROFILE", item);
        return Results.Ok(ToPerson(item));
    }

    static (string? personId, string? email) GetClaims(HttpContext ctx) =>
        (ctx.User.FindFirst("sub")?.Value, ctx.User.FindFirst("email")?.Value);

    static void SetList(Dictionary<string, AttributeValue> attrs, string key, List<string> values)
    {
        attrs[key] = new AttributeValue
        {
            L = values.Select(v => new AttributeValue(v)).ToList(),
            IsLSet = true,
        };
    }

    static List<string> GetList(Dictionary<string, AttributeValue> attrs, string key)
        => attrs.TryGetValue(key, out var v) ? v.L.Select(x => x.S).ToList() : [];

    static PersonProfile ToPerson(Dictionary<string, AttributeValue> attrs) =>
        new(
            attrs["PersonId"].S,
            attrs["Name"].S,
            attrs["Email"].S,
            attrs.TryGetValue("Bio", out var b) ? b.S : null,
            GetList(attrs, "Skills"),
            attrs.TryGetValue("IsAdmin", out var a) && a.BOOL,
            attrs["CreatedAt"].S,
            attrs.TryGetValue("UpdatedAt", out var ua) ? ua.S : null);
}

record PatchMeRequest(
    string? Name,
    string? Bio,
    List<string>? Skills);

record PersonProfile(
    string PersonId,
    string Name,
    string Email,
    string? Bio,
    List<string> Skills,
    bool IsAdmin,
    string CreatedAt,
    string? UpdatedAt);
