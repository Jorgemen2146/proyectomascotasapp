using System.Text;
using DogPlatform.Common.Extensions;
using DogPlatform.Logging;
using DogPlatform.Notification.API.Jobs;
using DogPlatform.Notification.API.WebSockets;
using DogPlatform.Notification.Application;
using DogPlatform.Notification.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDogPlatformSwagger("DogPlatform Notifications API");
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<INotificationWebSocketConnectionManager,
    NotificationWebSocketConnectionManager>();
builder.Services.AddSingleton<INotificationRealtimePublisher,
    WebSocketNotificationRealtimePublisher>();
builder.Services.AddDogPlatformHttpLogging(builder.Configuration, builder.Environment);

var jwt = builder.Configuration.GetSection("Jwt");
var secret = jwt["Secret"] ?? string.Empty;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var hasAuthorizationHeader =
                    !string.IsNullOrWhiteSpace(context.Request.Headers.Authorization);
                var accessToken = context.Request.Query["access_token"];
                if (!hasAuthorizationHeader && !string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/ws/notifications"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var reminderSection = builder.Configuration.GetSection("VaccinationNotifications");
if (reminderSection.GetValue("Enabled", true))
{
    var cronExpression = reminderSection["CronExpression"] ?? "0 0 8 * * ?";
    var timeZoneId = reminderSection["TimeZoneId"] ?? "SA Pacific Standard Time";
    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    builder.Services.AddQuartz(configuration =>
    {
        var jobKey = new JobKey(nameof(VaccinationReminderJob));
        configuration.AddJob<VaccinationReminderJob>(options => options.WithIdentity(jobKey));
        configuration.AddTrigger(options => options
            .ForJob(jobKey)
            .WithIdentity($"{nameof(VaccinationReminderJob)}-daily")
            .WithCronSchedule(cronExpression, schedule => schedule.InTimeZone(timeZone)));
    });
    builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
}

var app = builder.Build();
app.UseHttpsRedirection();
app.UseDogPlatformExceptionHandling();
app.UseDogPlatformRequestLogging();
app.UseDogPlatformSwagger("DogPlatform Notifications API v1");
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/ws/notifications", NotificationWebSocketEndpoint.HandleAsync)
    .RequireAuthorization();
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "DogPlatform.Notifications"
}));
app.Run();

public partial class Program;
