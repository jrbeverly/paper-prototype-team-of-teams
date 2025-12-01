using Amazon.DynamoDBv2.Model;

namespace Backend;

static class MembershipsEndpoints
{
    public static void MapMemberships(this WebApplication app)
    {
        app.MapPost("/teams/{id}/join", JoinTeam).RequireAuthorization();
        app.MapPost("/teams/{id}/leave", LeaveTeam).RequireAuthorization();
        app.MapGet("/me/membership", GetMyMembership).RequireAuthorization();
        app.MapPatch("/transfers/{id}", ActionTransfer).RequireAuthorization();
        app.MapDelete("/transfers/{id}", CancelTransfer).RequireAuthorization();
        app.MapGet("/transfers/pending", GetPendingTransfers).RequireAuthorization();
    }

    static async Task<IResult> JoinTeam(string id, HttpContext ctx, TableClient db)
    {
        var personId = ctx.User.FindFirst("sub")?.Value;
        if (personId is null) return Results.Unauthorized();

        var team = await db.GetAsync($"TEAM#{id}", "#PROFILE");
        if (team is null) return Results.NotFound();

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
        var existing = await db.GetAsync($"PERSON#{personId}", "MEMBERSHIP#ACTIVE");

        var membershipAttrs = new Dictionary<string, AttributeValue>
        {
            ["GSI1PK"] = new($"TEAM#{id}"),
            ["GSI1SK"] = new($"MEMBER#{personId}"),
            ["PersonId"] = new(personId),
            ["TeamId"] = new(id),
            ["JoinedAt"] = new(now),
        };

        if (existing is not null)
        {
            var currentTeamId = existing["TeamId"].S;
            if (currentTeamId == id)
                return Results.Ok(ToMembership(existing));

            var community = await db.GetAsync("COMMUNITY#default", "#CONFIG");
            var approvalRequired = community is not null &&
                community.TryGetValue("TransferApprovalDefault", out var tap) &&
                tap.S == "REQUIRED";

            if (approvalRequired)
            {
                var allTransfers = await db.QueryAsync($"PERSON#{personId}", "TRANSFER#");
                if (allTransfers.Any(t => t["ToTeamId"].S == id && t["Status"].S == "PENDING"))
                    return Results.Ok(ToMembership(existing));
            }

            var transferId = Guid.NewGuid().ToString("N")[..12];
            var transferAttrs = new Dictionary<string, AttributeValue>
            {
                ["TransferId"] = new(transferId),
                ["PersonId"] = new(personId),
                ["FromTeamId"] = new(currentTeamId),
                ["ToTeamId"] = new(id),
                ["Status"] = new(approvalRequired ? "PENDING" : "APPROVED"),
                ["RequestedAt"] = new(now),
            };

            if (approvalRequired)
            {
                await db.PutAsync($"PERSON#{personId}", $"TRANSFER#{transferId}", transferAttrs);
                return Results.Ok(ToMembership(existing));
            }

            await db.TransactPutAsync([
                ($"PERSON#{personId}", "MEMBERSHIP#ACTIVE", membershipAttrs),
                ($"PERSON#{personId}", $"TRANSFER#{transferId}", transferAttrs),
            ]);
        }
        else
        {
            await db.PutAsync($"PERSON#{personId}", "MEMBERSHIP#ACTIVE", membershipAttrs);
        }

        return Results.Ok(ToMembership(membershipAttrs));
    }

    static async Task<IResult> LeaveTeam(string id, HttpContext ctx, TableClient db)
    {
        var personId = ctx.User.FindFirst("sub")?.Value;
        if (personId is null) return Results.Unauthorized();

        var existing = await db.GetAsync($"PERSON#{personId}", "MEMBERSHIP#ACTIVE");
        if (existing is null)
            return Results.NotFound(new { error = "No active membership" });
        if (existing["TeamId"].S != id)
            return Results.BadRequest(new { error = "Not a member of this team" });

        await db.DeleteAsync($"PERSON#{personId}", "MEMBERSHIP#ACTIVE");
        return Results.NoContent();
    }

    static async Task<IResult> GetMyMembership(HttpContext ctx, TableClient db)
    {
        var personId = ctx.User.FindFirst("sub")?.Value;
        if (personId is null) return Results.Unauthorized();

        var membershipTask = db.GetAsync($"PERSON#{personId}", "MEMBERSHIP#ACTIVE");
        var transfersTask = db.QueryAsync($"PERSON#{personId}", "TRANSFER#");

        await Task.WhenAll(membershipTask, transfersTask);

        var membership = membershipTask.Result is not null ? ToMembership(membershipTask.Result) : null;
        var transfers = transfersTask.Result
            .OrderByDescending(t => t["RequestedAt"].S)
            .Select(ToTransfer)
            .ToList();

        return Results.Ok(new MyMembership(membership, transfers));
    }

    static async Task<IResult> ActionTransfer(string id, ActionTransferRequest req, HttpContext ctx, TableClient db)
    {
        var authError = await AdminHelper.RequireAdmin(ctx, db);
        if (authError is not null) return authError;

        if (req.Action is not ("APPROVE" or "REJECT"))
            return Results.BadRequest(new { error = "Action must be APPROVE or REJECT" });

        var matches = await db.ScanByPkPrefixAsync("PERSON#", $"TRANSFER#{id}");
        if (matches.Count == 0)
            return Results.NotFound();

        var transfer = matches[0];
        if (transfer["Status"].S != "PENDING")
            return Results.BadRequest(new { error = "Transfer is not pending" });

        var personId = transfer["PersonId"].S;
        var toTeamId = transfer["ToTeamId"].S;
        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";

        transfer["Status"] = new(req.Action == "APPROVE" ? "APPROVED" : "REJECTED");
        transfer["ResolvedAt"] = new(now);

        if (req.Action == "APPROVE")
        {
            var toTeam = await db.GetAsync($"TEAM#{toTeamId}", "#PROFILE");
            if (toTeam is null) return Results.NotFound();

            var membershipAttrs = new Dictionary<string, AttributeValue>
            {
                ["GSI1PK"] = new($"TEAM#{toTeamId}"),
                ["GSI1SK"] = new($"MEMBER#{personId}"),
                ["PersonId"] = new(personId),
                ["TeamId"] = new(toTeamId),
                ["JoinedAt"] = new(now),
            };

            await db.TransactPutAsync([
                ($"PERSON#{personId}", "MEMBERSHIP#ACTIVE", membershipAttrs),
                ($"PERSON#{personId}", $"TRANSFER#{id}", transfer),
            ]);
        }
        else
        {
            await db.PutAsync($"PERSON#{personId}", $"TRANSFER#{id}", transfer);
        }

        return Results.Ok(ToTransfer(transfer));
    }

    static async Task<IResult> CancelTransfer(string id, HttpContext ctx, TableClient db)
    {
        var personId = ctx.User.FindFirst("sub")?.Value;
        if (personId is null) return Results.Unauthorized();

        var transfer = await db.GetAsync($"PERSON#{personId}", $"TRANSFER#{id}");
        if (transfer is null)
            return Results.NotFound();
        if (transfer["Status"].S != "PENDING")
            return Results.BadRequest(new { error = "Transfer is not pending" });

        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
        transfer["Status"] = new("CANCELLED");
        transfer["ResolvedAt"] = new(now);
        await db.PutAsync($"PERSON#{personId}", $"TRANSFER#{id}", transfer);

        return Results.Ok(ToTransfer(transfer));
    }

    static async Task<IResult> GetPendingTransfers(HttpContext ctx, TableClient db)
    {
        var authError = await AdminHelper.RequireAdmin(ctx, db);
        if (authError is not null) return authError;

        var transfers = await db.ScanAsync(
            "begins_with(SK, :skp) AND #s = :status",
            new Dictionary<string, AttributeValue>
            {
                [":skp"] = new("TRANSFER#"),
                [":status"] = new("PENDING"),
            },
            new Dictionary<string, string> { ["#s"] = "Status" });

        var enriched = await Task.WhenAll(transfers.Select(async t =>
        {
            var pid = t["PersonId"].S;
            var fromId = t["FromTeamId"].S;
            var toId = t["ToTeamId"].S;

            var personTask = db.GetAsync($"PERSON#{pid}", "#PROFILE");
            var fromTask = db.GetAsync($"TEAM#{fromId}", "#PROFILE");
            var toTask = db.GetAsync($"TEAM#{toId}", "#PROFILE");

            await Task.WhenAll(personTask, fromTask, toTask);

            return new PendingTransferDetail(
                t["TransferId"].S,
                pid,
                personTask.Result?["Name"].S ?? pid,
                fromId,
                fromTask.Result?["Name"].S ?? fromId,
                toId,
                toTask.Result?["Name"].S ?? toId,
                t["RequestedAt"].S);
        }));

        return Results.Ok(enriched.OrderBy(t => t.RequestedAt).ToList());
    }

    static MembershipDetail ToMembership(Dictionary<string, AttributeValue> attrs) =>
        new(attrs["TeamId"].S, attrs["JoinedAt"].S);

    static TransferRecord ToTransfer(Dictionary<string, AttributeValue> attrs) =>
        new(
            attrs["TransferId"].S,
            attrs["FromTeamId"].S,
            attrs["ToTeamId"].S,
            attrs["Status"].S,
            attrs["RequestedAt"].S,
            attrs.TryGetValue("ResolvedAt", out var r) ? r.S : null);
}

record MyMembership(MembershipDetail? ActiveTeam, List<TransferRecord> Transfers);
record MembershipDetail(string TeamId, string JoinedAt);
record TransferRecord(string TransferId, string FromTeamId, string ToTeamId, string Status, string RequestedAt, string? ResolvedAt);
record ActionTransferRequest(string Action);
record PendingTransferDetail(
    string TransferId,
    string PersonId,
    string PersonName,
    string FromTeamId,
    string FromTeamName,
    string ToTeamId,
    string ToTeamName,
    string RequestedAt);
