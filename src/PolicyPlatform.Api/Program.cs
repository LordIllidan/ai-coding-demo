using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PolicyPlatform.Api.Auth;
using PolicyPlatform.Api.Filters;
using PolicyPlatform.Application.Abstractions;
using PolicyPlatform.Domain.Assistance;
using PolicyPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddPolicyPlatformInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();
builder.Services.AddScoped<IdempotencyKeyRequiredFilter>();
builder.Services.AddScoped<AssistanceValidationExceptionFilter>();

// JWT bearer auth for assistance-report requests (AISDLC-57/68): the user and vehicle
// are identified from the token's claims, never from the request body. Signing key comes
// from configuration/user-secrets in real environments; an empty dev key simply rejects
// all tokens rather than crashing, matching the connection-string fallback pattern above.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        builder.Configuration.GetSection("Jwt").Bind(options);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"] ?? string.Empty)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        };
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                return WriteAssistanceAuthErrorAsync(context.Response, StatusCodes.Status401Unauthorized);
            },
            OnForbidden = context
                => WriteAssistanceAuthErrorAsync(context.Response, StatusCodes.Status403Forbidden),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();

// Writes the assistance-reports contract's AUTH_001 error shape for 401/403 responses,
// instead of ASP.NET's default empty-body challenge/forbid.
static Task WriteAssistanceAuthErrorAsync(HttpResponse response, int statusCode)
{
    response.StatusCode = statusCode;
    response.ContentType = "application/json";
    var message = statusCode == StatusCodes.Status401Unauthorized
        ? "Missing or invalid JWT."
        : "Caller is not permitted to perform this action.";
    return response.WriteAsync(JsonSerializer.Serialize(new { code = AssistanceErrorCodes.Unauthorized, message }));
}

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
