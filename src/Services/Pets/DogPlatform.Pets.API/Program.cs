using System.Text;
using DogPlatform.Authentication;
using DogPlatform.Common.Extensions;
using DogPlatform.Pets.Application;
using DogPlatform.Pets.Infrastructure;
using DogPlatform.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

const long maximumPhotoJsonRequestBytes = 16 * 1024 * 1024;
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = maximumPhotoJsonRequestBytes);
builder.Services.Configure<Microsoft.AspNetCore.Builder.IISServerOptions>(options =>
    options.MaxRequestBodySize = maximumPhotoJsonRequestBytes);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDogPlatformSwagger("DogPlatform Pets API");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDogPlatformHttpLogging(builder.Configuration, builder.Environment);

var jwtSection = builder.Configuration.GetSection("Jwt");
var secret = jwtSection["Secret"] ?? string.Empty;
var issuer = jwtSection["Issuer"] ?? string.Empty;
var audience = jwtSection["Audience"] ?? string.Empty;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
    })
    .AddInternalService(options =>
        options.ApiKey = builder.Configuration["InternalServices:ApiKey"] ?? string.Empty);

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseDogPlatformExceptionHandling();
app.UseDogPlatformRequestLogging();
app.UseDogPlatformSwagger("DogPlatform Pets API v1");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "DogPlatform.Pets"
}));

app.Run();

