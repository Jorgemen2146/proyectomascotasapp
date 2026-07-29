using DogPlatform.Common.Extensions;
using DogPlatform.Identity.Application;
using DogPlatform.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDogPlatformSwagger("DogPlatform Identity API");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseDogPlatformSwagger("DogPlatform Identity API v1");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "DogPlatform.Identity"
}));

app.Run();

