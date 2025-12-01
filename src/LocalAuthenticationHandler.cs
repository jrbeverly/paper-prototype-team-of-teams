using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Backend;

sealed class LocalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Local";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var personId = Request.Headers["X-Person-Id"].FirstOrDefault() ?? "p1";
        var identity = new ClaimsIdentity(
            [new Claim("sub", personId), new Claim("email", $"{personId}@local.test")],
            Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
