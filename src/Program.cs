using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Backend;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddSingleton<TableClient>();

var localDevelopment = builder.Configuration.GetValue<bool>("LOCAL_DEVELOPMENT");
var dynamoEndpoint = builder.Configuration["DYNAMODB_ENDPOINT"];
var userPoolId = builder.Configuration["COGNITO_USER_POOL_ID"] ?? "";
var clientId = builder.Configuration["COGNITO_CLIENT_ID"] ?? "";
var region = builder.Configuration["AWS_REGION"] ?? "us-east-1";

if (dynamoEndpoint is not null)
{
    builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(
        new BasicAWSCredentials("local", "local"),
        new AmazonDynamoDBConfig
        {
            ServiceURL = dynamoEndpoint,
            AuthenticationRegion = region,
        }));
}
else
{
    builder.Services.AddAWSService<IAmazonDynamoDB>();
}

if (localDevelopment)
{
    builder.Services
        .AddAuthentication(LocalAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(
            LocalAuthenticationHandler.SchemeName, _ => { });
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.Authority = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}";
            options.TokenValidationParameters = new()
            {
                ValidateAudience = true,
                ValidAudience = clientId,
            };
        });
}
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapMe();
app.MapTeams();
app.MapCommunity();
app.MapPeople();
app.MapMemberships();
app.MapObservations();
app.MapRoles();

app.Run();
