using Amazon.DynamoDBv2.Model;

namespace Backend;

static class PeopleEndpoints
{
    public static void MapPeople(this WebApplication app)
    {
        app.MapGet("/people", ListPeople).RequireAuthorization();
        app.MapGet("/people/{id}", GetPerson).RequireAuthorization();
    }

    static async Task<IResult> ListPeople(TableClient db)
    {
        var items = await db.ScanByPkPrefixAsync("PERSON#", "#PROFILE");
        return Results.Ok(items.Select(ToSummary).ToList());
    }

    static async Task<IResult> GetPerson(string id, TableClient db)
    {
        var item = await db.GetAsync($"PERSON#{id}", "#PROFILE");
        if (item is null) return Results.NotFound();
        return Results.Ok(ToSummary(item));
    }

    static List<string> GetList(Dictionary<string, AttributeValue> attrs, string key)
        => attrs.TryGetValue(key, out var v) ? v.L.Select(x => x.S).ToList() : [];

    static PersonSummary ToSummary(Dictionary<string, AttributeValue> attrs) =>
        new(
            attrs["PersonId"].S,
            attrs["Name"].S,
            attrs.TryGetValue("Bio", out var b) ? b.S : null,
            GetList(attrs, "Skills"));
}

record PersonSummary(
    string PersonId,
    string Name,
    string? Bio,
    List<string> Skills);
