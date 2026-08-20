using DogPlatform.Common.Extensions;
using DogPlatform.Identity.Application;
using DogPlatform.Identity.Infrastructure;
using DogPlatform.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDogPlatformSwagger("DogPlatform Identity API");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDogPlatformHttpLogging(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseHttpsRedirection();

app.UseDogPlatformExceptionHandling();
app.UseDogPlatformRequestLogging();
app.UseDogPlatformSwagger("DogPlatform Identity API v1");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "DogPlatform.Identity"
}));

app.Run();

