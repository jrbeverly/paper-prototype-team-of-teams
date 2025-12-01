using Amazon.DynamoDBv2.Model;

namespace Backend;

static class AdminHelper
{
    public static async Task<IResult?> RequireAdmin(HttpContext ctx, TableClient db)
    {
        var personId = ctx.Request.Headers["X-Person-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(personId))
            return Results.Json(new { error = "X-Person-Id header is required" }, statusCode: 401);

        var person = await db.GetAsync($"PERSON#{personId}", "#PROFILE");
        if (person is null)
            return Results.Json(new { error = "Person not found" }, statusCode: 401);

        if (!person.TryGetValue("IsAdmin", out var isAdmin) || !isAdmin.BOOL)
            return Results.Json(new { error = "Admin access required" }, statusCode: 403);

        return null;
    }
}
