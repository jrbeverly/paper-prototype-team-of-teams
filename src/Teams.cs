using Amazon.DynamoDBv2.Model;

namespace Backend;

static class TeamsEndpoints
{
    public static void MapTeams(this WebApplication app)
    {
        app.MapPost("/teams", CreateTeam);
        app.MapPatch("/teams/{id}", PatchTeam);
        app.MapGet("/teams", ListTeams);
        app.MapGet("/teams/{id}", GetTeam);
        app.MapDelete("/teams/{id}", DeleteTeam);
    }

    static async Task<IResult> CreateTeam(CreateTeamRequest req, TableClient db)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.BadRequest(new { error = "Name is required" });

        var teamId = Guid.NewGuid().ToString("N")[..12];
        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";

        var attrs = new Dictionary<string, AttributeValue>
        {
            ["GSI1PK"] = new("TEAMS"),
            ["GSI1SK"] = new($"TEAM#{teamId}"),
            ["TeamId"] = new(teamId),
            ["Name"] = new(req.Name.Trim()),
            ["CreatedAt"] = new(now),
        };
        SetOptional(attrs, "Purpose", req.Purpose);
        SetOptional(attrs, "Mission", req.Mission);
        SetList(attrs, "Objectives", req.Objectives ?? []);
        SetList(attrs, "Skills", req.Skills ?? []);
        SetList(attrs, "Responsibilities", req.Responsibilities ?? []);
        SetList(attrs, "Commitments", req.Commitments ?? []);
        SetList(attrs, "OpenPositions", req.OpenPositions ?? []);

        await db.PutAsync($"TEAM#{teamId}", "#PROFILE", attrs);

        return Results.Created($"/teams/{teamId}", ToDetail(attrs, [], []));
    }

    static async Task<IResult> PatchTeam(string id, PatchTeamRequest req, TableClient db)
    {
        var item = await db.GetAsync($"TEAM#{id}", "#PROFILE");
        if (item is null)
            return Results.NotFound();

        if (req.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name cannot be blank" });
            item["Name"] = new(req.Name.Trim());
        }
        if (req.Purpose is not null) item["Purpose"] = new(req.Purpose);
        if (req.Mission is not null) item["Mission"] = new(req.Mission);
        if (req.Objectives is not null) SetList(item, "Objectives", req.Objectives);
        if (req.Skills is not null) SetList(item, "Skills", req.Skills);
        if (req.Responsibilities is not null) SetList(item, "Responsibilities", req.Responsibilities);
        if (req.Commitments is not null) SetList(item, "Commitments", req.Commitments);
        if (req.OpenPositions is not null) SetList(item, "OpenPositions", req.OpenPositions);

        await db.PutAsync($"TEAM#{id}", "#PROFILE", item);

        return Results.Ok(ToDetail(item, [], []));
    }

    static async Task<IResult> ListTeams(TableClient db)
    {
        var items = await db.QueryGsi1Async("TEAMS");
        return Results.Ok(items.Select(ToSummary).ToList());
    }

    static async Task<IResult> GetTeam(string id, TableClient db)
    {
        var profileTask = db.GetAsync($"TEAM#{id}", "#PROFILE");
        var membersTask = db.QueryGsi1Async($"TEAM#{id}", "MEMBER#");
        var rolesTask = db.QueryAsync($"TEAM#{id}", "ROLE#");

        await Task.WhenAll(profileTask, membersTask, rolesTask);

        var profile = profileTask.Result;
        if (profile is null)
            return Results.NotFound();

        var memberItems = membersTask.Result;
        var personItems = await Task.WhenAll(
            memberItems.Select(m => db.GetAsync($"PERSON#{m["PersonId"].S}", "#PROFILE")));

        var members = memberItems.Select((m, i) =>
        {
            var personId = m["PersonId"].S;
            return new TeamMember(personId, personItems[i]?["Name"].S ?? personId, m["JoinedAt"].S);
        }).ToList();

        var roles = rolesTask.Result
            .Select(r => new TeamRole(r["PersonId"].S, r["RoleName"].S, r["AssignedAt"].S))
            .ToList();

        return Results.Ok(ToDetail(profile, members, roles));
    }

    static async Task<IResult> DeleteTeam(string id, TableClient db, HttpContext ctx)
    {
        var authError = await AdminHelper.RequireAdmin(ctx, db);
        if (authError is not null) return authError;

        var item = await db.GetAsync($"TEAM#{id}", "#PROFILE");
        if (item is null)
            return Results.NotFound();

        await db.DeleteAsync($"TEAM#{id}", "#PROFILE");
        return Results.NoContent();
    }

    static void SetOptional(Dictionary<string, AttributeValue> attrs, string key, string? value)
    {
        if (value is not null)
            attrs[key] = new(value);
    }

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

    static string? GetString(Dictionary<string, AttributeValue> attrs, string key)
        => attrs.TryGetValue(key, out var v) ? v.S : null;

    static TeamSummary ToSummary(Dictionary<string, AttributeValue> attrs) =>
        new(
            attrs["TeamId"].S,
            attrs["Name"].S,
            GetString(attrs, "Purpose"),
            GetList(attrs, "Skills"),
            GetList(attrs, "OpenPositions"));

    static TeamDetail ToDetail(
        Dictionary<string, AttributeValue> attrs,
        List<TeamMember> members,
        List<TeamRole> roles) =>
        new(
            attrs["TeamId"].S,
            attrs["Name"].S,
            GetString(attrs, "Purpose"),
            GetString(attrs, "Mission"),
            GetList(attrs, "Objectives"),
            GetList(attrs, "Skills"),
            GetList(attrs, "Responsibilities"),
            GetList(attrs, "Commitments"),
            GetList(attrs, "OpenPositions"),
            attrs.TryGetValue("CreatedAt", out var ca) ? ca.S : "",
            members,
            roles);
}

record CreateTeamRequest(
    string Name,
    string? Purpose,
    string? Mission,
    List<string>? Objectives,
    List<string>? Skills,
    List<string>? Responsibilities,
    List<string>? Commitments,
    List<string>? OpenPositions);

record PatchTeamRequest(
    string? Name,
    string? Purpose,
    string? Mission,
    List<string>? Objectives,
    List<string>? Skills,
    List<string>? Responsibilities,
    List<string>? Commitments,
    List<string>? OpenPositions);

record TeamSummary(
    string TeamId,
    string Name,
    string? Purpose,
    List<string> Skills,
    List<string> OpenPositions);

record TeamMember(string PersonId, string Name, string JoinedAt);

record TeamRole(string PersonId, string RoleName, string AssignedAt);

record TeamDetail(
    string TeamId,
    string Name,
    string? Purpose,
    string? Mission,
    List<string> Objectives,
    List<string> Skills,
    List<string> Responsibilities,
    List<string> Commitments,
    List<string> OpenPositions,
    string CreatedAt,
    List<TeamMember> Members,
    List<TeamRole> Roles);
