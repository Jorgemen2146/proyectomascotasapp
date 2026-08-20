using System.Text;
using DogPlatform.Common.Extensions;
using DogPlatform.Genealogy.Application;
using DogPlatform.Genealogy.Infrastructure;
using DogPlatform.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDogPlatformSwagger("DogPlatform Genealogy API");

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDogPlatformHttpLogging(builder.Configuration, builder.Environment);

var jwtSection = builder.Configuration.GetSection("Jwt");
var secret     = jwtSection["Secret"]   ?? string.Empty;
var issuer     = jwtSection["Issuer"]   ?? string.Empty;
var audience   = jwtSection["Audience"] ?? string.Empty;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = issuer,
            ValidAudience            = audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseDogPlatformExceptionHandling();
app.UseDogPlatformRequestLogging();
app.UseDogPlatformSwagger("DogPlatform Genealogy API v1");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status  = "Healthy",
    service = "DogPlatform.Genealogy"
}));

app.Run();
