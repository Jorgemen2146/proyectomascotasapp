using DogPlatform.Identity.Application;
using DogPlatform.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "DogPlatform.Identity"
}));

app.Run();
