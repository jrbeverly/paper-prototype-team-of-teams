using Amazon.DynamoDBv2.Model;

namespace Backend;

static class CommunityEndpoints
{
    const string CommunityPk = "COMMUNITY#default";
    const string CommunitySk = "#CONFIG";

    public static void MapCommunity(this WebApplication app)
    {
        app.MapGet("/community", GetCommunity);
        app.MapPatch("/community", PatchCommunity);
    }

    static async Task<IResult> GetCommunity(TableClient db)
    {
        var item = await db.GetAsync(CommunityPk, CommunitySk);
        if (item is null)
            return Results.NotFound();
        return Results.Ok(ToConfig(item));
    }

    static async Task<IResult> PatchCommunity(
        PatchCommunityRequest req, TableClient db, HttpContext ctx)
    {
        var authError = await AdminHelper.RequireAdmin(ctx, db);
        if (authError is not null) return authError;

        var item = await db.GetAsync(CommunityPk, CommunitySk);
        if (item is null)
            return Results.NotFound();

        if (req.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name cannot be blank" });
            item["Name"] = new(req.Name.Trim());
        }
        if (req.Description is not null) item["Description"] = new(req.Description);
        if (req.TransferApprovalDefault is not null)
        {
            if (req.TransferApprovalDefault is not ("REQUIRED" or "NOT_REQUIRED"))
                return Results.BadRequest(new { error = "TransferApprovalDefault must be REQUIRED or NOT_REQUIRED" });
            item["TransferApprovalDefault"] = new(req.TransferApprovalDefault);
        }

        item["UpdatedAt"] = new(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z");

        await db.PutAsync(CommunityPk, CommunitySk, item);
        return Results.Ok(ToConfig(item));
    }

    static CommunityConfig ToConfig(Dictionary<string, AttributeValue> attrs) =>
        new(
            attrs["Name"].S,
            attrs.TryGetValue("Description", out var d) ? d.S : null,
            attrs.TryGetValue("TransferApprovalDefault", out var t) ? t.S : "NOT_REQUIRED",
            attrs["CreatedAt"].S,
            attrs.TryGetValue("UpdatedAt", out var ua) ? ua.S : null);
}

record PatchCommunityRequest(
    string? Name,
    string? Description,
    string? TransferApprovalDefault);

record CommunityConfig(
    string Name,
    string? Description,
    string TransferApprovalDefault,
    string CreatedAt,
    string? UpdatedAt);
